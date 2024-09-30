using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.ResourceManagement.Util
{
    internal class BinaryStorageBuffer
    {
        class BuiltinTypesSerializer :
            ISerializationAdapter<int>,
            ISerializationAdapter<bool>,
            ISerializationAdapter<long>,
            ISerializationAdapter<string>,
            ISerializationAdapter<Hash128>
        {
            public IEnumerable<ISerializationAdapter> Dependencies => null;

            struct ObjectToStringRemap
            {
                public uint stringId;
                public char separator;
            }

            public object Deserialize(Reader reader, Type t, uint offset)
            {
                if (offset == uint.MaxValue)
                    return null;
                if (t == typeof(int)) return reader.ReadValue<int>(offset);
                if (t == typeof(bool)) return reader.ReadValue<bool>(offset);
                if (t == typeof(long)) return reader.ReadValue<long>(offset);
                if (t == typeof(Hash128)) return reader.ReadValue<Hash128>(offset);
                if (t == typeof(string))
                {
                    var remap = reader.ReadValue<ObjectToStringRemap>(offset);
                    return reader.ReadString(remap.stringId, remap.separator, false);
                }
                return null;
            }

            char FindBestSeparator(string str, params char[] seps)
            {
                int bestCount = 0;
                char bestSep = (char)0;
                foreach (var s in seps)
                {
                    var sepCount = str.Count(c => c == s);
                    if (sepCount > bestCount)
                    {
                        bestCount = sepCount;
                        bestSep = s;
                    }
                }

                var parts = str.Split(bestSep);
                int validParts = 0;
                foreach (var p in parts)
                    if (p.Length > 8)
                        validParts++;

                if (validParts < 2)
                    bestSep = (char)0;

                return bestSep;
            }

            public uint Serialize(Writer writer, object val)
            {
                if (val == null)
                    return uint.MaxValue;

                var t = val.GetType();
                if (t == typeof(int)) return writer.Write((int)val);
                if (t == typeof(bool)) return writer.Write((bool)val);
                if (t == typeof(long)) return writer.Write((long)val);
                if (t == typeof(Hash128)) return writer.Write((Hash128)val);
                if (t == typeof(string))
                {
                    var str = val as string;
                    var bestSep = FindBestSeparator(str, '/', '.', '-', '_', '\\', ',');
                    return writer.Write(new ObjectToStringRemap { stringId = writer.WriteString((string)val), separator = bestSep });
                }
                return uint.MaxValue;
            }
        }

        class TypeSerializer :
             ISerializationAdapter<Type>
        {
            struct Data
            {
                public uint assemblyId;
                public uint classId;
            }
            public IEnumerable<BinaryStorageBuffer.ISerializationAdapter> Dependencies => null;

            public object Deserialize(Reader reader, Type type, uint offset)
            {
                try
                {
                    var d = reader.ReadValue<Data>(offset);
                    var assemblyName = reader.ReadString(d.assemblyId, '.');
                    var className = reader.ReadString(d.classId, '.');
                    var assembly = Assembly.Load(assemblyName);
                    return assembly == null ? null : assembly.GetType(className);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return null;
                }
            }

            public uint Serialize(Writer writer, object val)
            {
                if (val == null)
                    return uint.MaxValue;
                var t = val as Type;
                return writer.Write(new Data
                {
                    assemblyId = writer.WriteString(t.Assembly.FullName, '.'),
                    classId = writer.WriteString(t.FullName, '.')
                });
            }
        }

        private unsafe static void ComputeHash(void* pData, ulong size, Hash128* hash)
        {
            if (pData == null || size == 0)
            {
                *hash = default;
                return;
            }
            HashUnsafeUtilities.ComputeHash128(pData, size, hash);
        }

        static void AddSerializationAdapter(Dictionary<Type, ISerializationAdapter> serializationAdapters, ISerializationAdapter adapter, bool forceOverride = false)
        {
            bool added = false;
            foreach (var i in adapter.GetType().GetInterfaces())
            {
                if (i.IsGenericType && typeof(ISerializationAdapter).IsAssignableFrom(i))
                {
                    var aType = i.GenericTypeArguments[0];
                    if (serializationAdapters.ContainsKey(aType))
                    {
                        if (forceOverride)
                        {
                            var prevAdapter = serializationAdapters[aType];
                            serializationAdapters.Remove(aType);
//                            serializationAdapters.Add(aType, adapter);
                            serializationAdapters[aType] = adapter;
                            added = true;
                            Debug.Log($"Replacing adapter for type {aType}: {prevAdapter} -> {adapter}");
                        }
                        else
                        {
                            Debug.Log($"Failed to register adapter for type {aType}: {adapter}, {serializationAdapters[aType]} is already registered.");
                        }
                    }
                    else
                    {
                        serializationAdapters[aType] = adapter;
                        added = true;
                    }
                }
            }
            if (added)
            {
                var deps = adapter.Dependencies;
                if (deps != null)
                    foreach (var d in deps)
                        AddSerializationAdapter(serializationAdapters, d);
            }
        }

        static bool GetSerializationAdapter(Dictionary<Type, ISerializationAdapter> serializationAdapters, Type t, out ISerializationAdapter adapter)
        {
            if (!serializationAdapters.TryGetValue(t, out adapter))
            {
                foreach (var k in serializationAdapters)
                    if (k.Key.IsAssignableFrom(t))
                        return (adapter = k.Value) != null;
                Debug.LogError($"Unable to find serialization adapter for type {t}.");
            }
            return adapter != null;
        }

        const uint kUnicodeStringFlag = 0x80000000;
        const uint kDynamicStringFlag = 0x40000000;
        const uint kClearFlagsMask = 0x3fffffff;
        struct DynamicString
        {
            public uint stringId;
            public uint nextId;
        }

        struct ObjectTypeData
        {
            public uint typeId;
            public uint objectId;
        }

        public interface ISerializationAdapter
        {
            IEnumerable<ISerializationAdapter> Dependencies { get; }
            uint Serialize(Writer writer, object val);
            object Deserialize(Reader reader, Type t, uint offset);
        }

        public interface ISerializationAdapter<T> : ISerializationAdapter
        {
        }

        public unsafe class Reader
        {
            byte[] m_Buffer;
            Dictionary<Type, ISerializationAdapter> m_Adapters;
            LRUCache<uint, object> m_Cache;
            StringBuilder stringBuilder;

            private void Init(byte[] data, int maxCachedObjects, params ISerializationAdapter[] adapters)
            {
                m_Buffer = data;
                stringBuilder = new StringBuilder(1024);
                m_Cache = new LRUCache<uint, object>(maxCachedObjects);
                m_Adapters = new Dictionary<Type, ISerializationAdapter>();
                foreach (var a in adapters)
                    BinaryStorageBuffer.AddSerializationAdapter(m_Adapters, a);
                BinaryStorageBuffer.AddSerializationAdapter(m_Adapters, new TypeSerializer());
                BinaryStorageBuffer.AddSerializationAdapter(m_Adapters, new BuiltinTypesSerializer());
            }

            public void AddSerializationAdapter(ISerializationAdapter a)
            {
                BinaryStorageBuffer.AddSerializationAdapter(m_Adapters, a);
            }

            public Reader(byte[] data, int maxCachedObjects = 1024, params ISerializationAdapter[] adapters)
            {
                Init(data, maxCachedObjects, adapters);
            }

            internal byte[] GetBuffer()
            {
                return m_Buffer;
            }


            public Reader(Stream inputStream, uint bufferSize, int maxCachedObjects, params ISerializationAdapter[] adapters)
            {
                var data = new byte[bufferSize == 0 ? inputStream.Length : bufferSize];
                inputStream.Read(data, 0, data.Length);
                Init(data, maxCachedObjects, adapters);
            }

            bool TryGetCachedValue<T>(uint offset, out T val)
            {
                if(m_Cache.TryGet(offset, out var obj))
                {
                    val = (T)obj;
                    return true;
                }
                val = default;
                return false;
            }

            public object[] ReadObjectArray(uint id, bool cacheValues = true)
            {
                if (id == uint.MaxValue)
                    return null;
                var ids = ReadValueArray<uint>(id, cacheValues);
                var objs = new object[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                    objs[i] = ReadObject(ids[i], cacheValues);
                return objs;
            }

            public object[] ReadObjectArray(Type t, uint id, bool cacheValues = true)
            {
                if (id == uint.MaxValue)
                    return null;
                var ids = ReadValueArray<uint>(id, cacheValues);
                var objs = new object[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                    objs[i] = ReadObject(t, ids[i], cacheValues);
                return objs;
            }

            public T[] ReadObjectArray<T>(uint id, bool cacheValues = true)
            {
                if (id == uint.MaxValue)
                    return null;
                var ids = ReadValueArray<uint>(id, cacheValues);
                var objs = new T[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                    objs[i] = ReadObject<T>(ids[i], cacheValues);
                return objs;
            }


            public object ReadObject(uint id, bool cacheValue = true)
            {
                if (id == uint.MaxValue)
                    return null;
                var td = ReadValue<ObjectTypeData>(id);
                var type = ReadObject<Type>(td.typeId);
                return ReadObject(type, td.objectId, cacheValue);
            }

            public T ReadObject<T>(uint offset, bool cacheValue = true) => (T)ReadObject(typeof(T), offset, cacheValue);

            public object ReadObject(Type t, uint id, bool cacheValue = true)
            {
                if (id == uint.MaxValue)
                    return null;

                if (TryGetCachedValue<object>(id, out var val))
                    return val;

                if (!GetSerializationAdapter(m_Adapters, t, out var adapter))
                    return null;
                object res = default;
                try
                {
                    res = adapter.Deserialize(this, t, id);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return null;
                }

                if (cacheValue && res != null)
                    m_Cache.TryAdd(id, res);

                return res;
            }

            public T[] ReadValueArray<T>(uint id, bool cacheValue = true) where T : unmanaged
            {
                if (id == uint.MaxValue)
                    return null;
                if (id - sizeof(uint) >= m_Buffer.Length)
                    throw new Exception($"Data offset {id} is out of bounds of buffer with length of {m_Buffer.Length}.");

                fixed (byte* pData = &m_Buffer[id - sizeof(uint)])
                {
                    if (TryGetCachedValue<T[]>(id, out var vals))
                        return vals;
                    uint size = 0;
                    UnsafeUtility.MemCpy(&size, pData, sizeof(uint));
                    if((id + size) > m_Buffer.Length)
                        throw new Exception($"Data size {size} is out of bounds of buffer with length of {m_Buffer.Length}.");
                    var elCount = size / sizeof(T);
                    var valsT = new T[elCount];
                    fixed (T* pVals = valsT)
                        UnsafeUtility.MemCpy(pVals, &pData[sizeof(uint)], size);
                    if(cacheValue)
                        m_Cache.TryAdd(id, valsT);
                    return valsT;
                }
            }

            public T ReadValue<T>(uint id) where T : unmanaged
            {
                if (id == uint.MaxValue)
                    return default;
                if (id >= m_Buffer.Length)
                    throw new Exception($"Data offset {id} is out of bounds of buffer with length of {m_Buffer.Length}.");

                fixed (byte* pData = m_Buffer)
                {
                    T val;
                    UnsafeUtility.MemCpy(&val, pData + id, sizeof(T));
                    return val;
                }
            }

            public string ReadString(uint id, char sep = (char)0, bool cacheValue = true)
            {
                if (id == uint.MaxValue)
                    return null;
                if (sep == (char)0)
                    return ReadAutoEncodedString(id, cacheValue);
                return ReadDynamicString(id, sep, cacheValue);
            }

            string ReadStringInternal(uint offset, Encoding enc, bool cacheValue = true)
            {
                if (offset - sizeof(uint) >= m_Buffer.Length)
                    throw new Exception($"Data offset {offset} is out of bounds of buffer with length of {m_Buffer.Length}.");

                if (TryGetCachedValue<string>(offset, out var val))
                    return val;

                fixed (byte* pData = m_Buffer)
                {
                    var strDataLength = *(uint*)&pData[offset - sizeof(uint)];
                    if (offset + strDataLength > m_Buffer.Length)
                        throw new Exception($"Data offset {offset}, len {strDataLength} is out of bounds of buffer with length of {m_Buffer.Length}.");

                    var valStr = enc.GetString(&pData[offset], (int)strDataLength);
                    if(cacheValue)
                        m_Cache.TryAdd(offset, valStr);
                    return valStr;
                }
            }

            string ReadAutoEncodedString(uint id, bool cacheValue)
            {
                if ((id & kUnicodeStringFlag) == kUnicodeStringFlag)
                    return ReadStringInternal((uint)(id & kClearFlagsMask), Encoding.Unicode, cacheValue);
                return ReadStringInternal(id, Encoding.ASCII, cacheValue);
            }

            string ReadDynamicString(uint id, char sep, bool cacheValue)
            {
                if ((id & kDynamicStringFlag) == kDynamicStringFlag)
                {
                    if (!TryGetCachedValue<string>(id, out var str))
                    {
                        var ds = ReadValue<DynamicString>((uint)(id & kClearFlagsMask));
                        stringBuilder.Append(ReadAutoEncodedString(ds.stringId, cacheValue));
                        while (ds.nextId != uint.MaxValue)
                        {
                            ds = ReadValue<DynamicString>(ds.nextId);
                            stringBuilder.Append(sep);
                            stringBuilder.Append(ReadAutoEncodedString(ds.stringId, cacheValue));
                        }
                        str = stringBuilder.ToString();
                        stringBuilder.Clear();
                        if (cacheValue)
                            m_Cache.TryAdd(id, str);
                    }
                    return str;
                }
                else
                {
                    return ReadAutoEncodedString(id, cacheValue);
                }
            }
        }

        public unsafe class Writer
        {
            class Chunk
            {
                public uint position;
                public byte[] data;
            }

            uint totalBytes;
            uint defaulChunkSize;
            List<Chunk> chunks;
            Dictionary<Hash128, uint> existingValues;
            Dictionary<Type, ISerializationAdapter> serializationAdapters;
            public uint Length => totalBytes;

            public Writer(int chunkSize = 1024*1024, params ISerializationAdapter[] adapters)
            {
                defaulChunkSize = (uint)(chunkSize > 0 ? chunkSize : 1024 * 1024);
                existingValues = new Dictionary<Hash128, uint>();
                chunks = new List<Chunk>(10);
                chunks.Add(new Chunk { position = 0 });
                serializationAdapters = new Dictionary<Type, ISerializationAdapter>();
                AddSerializationAdapter(serializationAdapters, new TypeSerializer());
                AddSerializationAdapter(serializationAdapters, new BuiltinTypesSerializer());
                foreach (var a in adapters)
                    AddSerializationAdapter(serializationAdapters, a, true);
            }


            Chunk FindChunkWithSpace(uint length)
            {
                var chunk = chunks[chunks.Count - 1];
                if (chunk.data == null)
                    chunk.data = new byte[length > defaulChunkSize ? length : defaulChunkSize];

                if (length > chunk.data.Length - chunk.position)
                {
                    chunk = new Chunk { position = 0, data = new byte[length > defaulChunkSize ? length : defaulChunkSize] };
                    chunks.Add(chunk);
                }
                return chunk;
            }

            uint WriteInternal(void* pData, uint dataSize, bool prefixSize)
            {
                Hash128 hash;
                ComputeHash(pData, (ulong)dataSize, &hash);
                if (existingValues.TryGetValue(hash, out var existingOffset))
                    return existingOffset;

                var addedBytes = prefixSize ? dataSize + sizeof(uint) : dataSize;
                var chunk = FindChunkWithSpace(addedBytes);
                fixed (byte* pChunkData = &chunk.data[chunk.position])
                {
                    var id = totalBytes;
                    if (prefixSize)
                    {
                        UnsafeUtility.MemCpy(pChunkData, &dataSize, sizeof(uint));
                        if(dataSize > 0)
                            UnsafeUtility.MemCpy(&pChunkData[sizeof(uint)], pData, dataSize);
                        id += sizeof(uint);
                    }
                    else
                    {
                        if (dataSize == 0)
                            return uint.MaxValue;
                        UnsafeUtility.MemCpy(pChunkData, pData, dataSize);
                    }

                    totalBytes += addedBytes;
                    chunk.position += addedBytes;
                    existingValues[hash] = id;
                    return id;
                }
            }

            uint ReserveInternal(uint dataSize, bool prefixSize)
            {
                //reserved data cannot reuse previously hashed values, but it can be reused for future writes
                var addedBytes = prefixSize ? dataSize + sizeof(uint) : dataSize;
                var chunk = FindChunkWithSpace(addedBytes);


                totalBytes += addedBytes;
                chunk.position += addedBytes;
                return totalBytes - dataSize;
            }

            void WriteInternal(uint id, void* pData, uint dataSize, bool prefixSize)
            {
                //reserved data cannot reuse previously hashed values, but it can be reused for future writes
                var addedBytes = prefixSize ? dataSize + sizeof(uint) : dataSize;
                Hash128 hash;
                ComputeHash(pData, (ulong)dataSize, &hash);
                existingValues[hash] = id;
                var chunkOffset = id;
                foreach (var c in chunks)
                {
                    if (chunkOffset < c.position)
                    {
                        fixed (byte* pChunkData = c.data)
                        {
                            if (prefixSize)
                                UnsafeUtility.MemCpy(&pChunkData[chunkOffset - sizeof(uint)], &dataSize, sizeof(uint));
                            UnsafeUtility.MemCpy(&pChunkData[chunkOffset], pData, dataSize);
                            return;
                        }
                    }
                    chunkOffset -= c.position;
                }
            }

            public uint Reserve<T>() where T : unmanaged => ReserveInternal((uint)sizeof(T), false);

            public uint Write<T>(in T val) where T : unmanaged
            {
                fixed (T* pData = &val)
                    return WriteInternal(pData, (uint)sizeof(T), false);
            }

            public uint Write<T>(T val) where T : unmanaged
            {
                return WriteInternal(&val, (uint)sizeof(T), false);
            }

            public uint Write<T>(uint offset, in T val) where T : unmanaged
            {
                fixed (T* pData = &val)
                    WriteInternal(offset, pData, (uint)sizeof(T), false);
                return offset;
            }

            public uint Write<T>(uint offset, T val) where T : unmanaged
            {
                WriteInternal(offset, &val, (uint)sizeof(T), false);
                return offset;
            }

            public uint Reserve<T>(uint count) where T : unmanaged => ReserveInternal((uint)sizeof(T) * count, true);

            public uint Write<T>(T[] values, bool hashElements = true) where T : unmanaged
            {
                fixed (T* pData = values)
                {
                    uint size = (uint)(values.Length * sizeof(T));
                    Hash128 hash;
                    ComputeHash(pData, (ulong)size, &hash);
                    if (existingValues.TryGetValue(hash, out var existingOffset))
                        return existingOffset;
                    var chunk = FindChunkWithSpace(size + sizeof(uint));
                    fixed (byte* pChunkData = &chunk.data[chunk.position])
                    {
                        var id = totalBytes + sizeof(uint);
                        UnsafeUtility.MemCpy(pChunkData, &size, sizeof(uint));
                        UnsafeUtility.MemCpy(&pChunkData[sizeof(uint)], pData, size);
                        var addedBytes = size + sizeof(uint);


                        totalBytes += addedBytes;
                        chunk.position += addedBytes;
                        existingValues[hash] = id;
                        if (hashElements && sizeof(T) > sizeof(uint))
                        {
                            for (int i = 0; i < values.Length; i++)
                            {
                                hash = default;
                                ComputeHash(&pData[i], (ulong)sizeof(T), &hash);
                                existingValues[hash] = id + (uint)(i * sizeof(T));
                            }
                        }
                        return id;
                    }
                }
            }

            public uint Write<T>(uint offset, T[] values, bool hashElements = true) where T : unmanaged
            {
                var dataSize = (uint)(values.Length * sizeof(T));
                var chunkOffset = offset;
                fixed (T* pValues = values)
                {
                    foreach (var c in chunks)
                    {
                        if (chunkOffset < c.position)
                        {
                            fixed (byte* pChunkData = c.data)
                            {
                                UnsafeUtility.MemCpy(&pChunkData[chunkOffset - sizeof(uint)], &dataSize, sizeof(uint));
                                UnsafeUtility.MemCpy(&pChunkData[chunkOffset], pValues, dataSize);

                                if (hashElements && sizeof(T) > sizeof(uint))
                                {
                                    for (int i = 0; i < values.Length; i++)
                                    {
                                        Hash128 hash;
                                        var v = values[i];
                                        ComputeHash(&v, (ulong)sizeof(T), &hash);
                                        existingValues[hash] = offset + (uint)i * (uint)sizeof(T);
                                    }
                                }
                                return offset;
                            }
                        }
                        chunkOffset -= c.position;
                    }
                }
                return uint.MaxValue;
            }

            public uint WriteObjects<T>(IEnumerable<T> objs, bool serizalizeTypeData)
            {
                if (objs == null)
                    return uint.MaxValue;
                var count = objs.Count();
                var ids = new uint[count];
                var index = 0;
                foreach (var o in objs)
                    ids[index++] = WriteObject(o, serizalizeTypeData);
                return Write(ids);
            }

            public uint WriteObject(object obj, bool serializeTypeData)
            {
                if (obj == null)
                    return uint.MaxValue;
                var objType = obj.GetType();

                if (!GetSerializationAdapter(serializationAdapters, objType, out var adapter))
                    return uint.MaxValue;

                var id = adapter.Serialize(this, obj);
                if (serializeTypeData)
                    id = Write(new ObjectTypeData { typeId = WriteObject(objType, false), objectId = id });

                return id;
            }

            public uint WriteString(string str, char sep = (char)0)
            {
                if (str == null)
                    return uint.MaxValue;

                return sep == (char)0 ? WriteAutoEncodedString(str) : WriteDynamicString(str, sep);
            }

            uint WriteStringInternal(string val, Encoding enc)
            {
                if (val == null)
                    return uint.MaxValue;
                byte[] tmp = enc.GetBytes(val);
                fixed (byte* pBytes = tmp)
                    return WriteInternal(pBytes, (uint)tmp.Length, true);
            }

            public byte[] SerializeToByteArray()
            {
                var data = new byte[totalBytes];
                fixed (byte* pData = data)
                {
                    uint offset = 0;
                    foreach (var c in chunks)
                    {
                        fixed (byte* pChunk = c.data)
                            UnsafeUtility.MemCpy(&pData[offset], pChunk, c.position);
                        offset += c.position;
                    }
                }
                return data;
            }

            public uint SerializeToStream(Stream str)
            {
                foreach (var c in chunks)
                    str.Write(c.data, 0, (int)c.position);
                return totalBytes;
            }

            static bool IsUnicode(string str)
            {
                for (int i = 0; i < str.Length; i++)
                    if (str[i] > 255)
                        return true;
                return false;
            }

            uint WriteAutoEncodedString(string str)
            {
                if (str == null)
                    return uint.MaxValue;
                if (IsUnicode(str))
                    return WriteUnicodeString(str);
                else
                    return WriteStringInternal(str, Encoding.ASCII);
            }

            uint WriteUnicodeString(string str)
            {
                var id = WriteStringInternal(str, Encoding.Unicode);
                return (kUnicodeStringFlag | id);
            }

            static uint ComputeStringSize(string str, out bool isUnicode)
            {
                if (isUnicode = IsUnicode(str))
                    return (uint)Encoding.Unicode.GetByteCount(str);
                return (uint)Encoding.ASCII.GetByteCount(str);
            }

            uint RecurseDynamicStringParts(StringParts[] parts, int index, char sep, uint minSize)
            {
                while (index < parts.Length - 1)
                {
                    var currPartSize = parts[index].dataSize;
                    var nextPartSize = parts[index + 1].dataSize;
                    if (currPartSize < minSize || nextPartSize < minSize)
                    {
                        parts[index + 1].str = $"{parts[index].str}{sep}{parts[index + 1].str}";
                        index++;
                    }
                    else
                    {
                        break;
                    }
                }
                var strId = parts[index].isUnicode ? WriteUnicodeString(parts[index].str) : WriteStringInternal(parts[index].str, Encoding.ASCII);
                var nxtId = (index < parts.Length - 1 ? RecurseDynamicStringParts(parts, index + 1, sep, minSize) : uint.MaxValue);
                var id = Write(new DynamicString { stringId = strId, nextId = nxtId });
                return id;
            }

            struct StringParts
            {
                public string str;
                public uint dataSize;
                public bool isUnicode;
            }

            unsafe uint WriteDynamicString(string str, char sep)
            {
                if (str == null)
                    return uint.MaxValue;
                var minSize = (uint)sizeof(DynamicString);
                var split = str.Split(sep);
                var parts = new StringParts[split.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    var partSize = ComputeStringSize(split[i], out var isUnicode);
                    parts[i] = new StringParts { str = split[i], dataSize = partSize, isUnicode = isUnicode };
                }

                if (parts.Length < 2 || (parts.Length == 2 && (parts[0].dataSize + parts[1].dataSize) < minSize))
                {
                    return WriteAutoEncodedString(str);
                }
                else
                {
                    return (kDynamicStringFlag | RecurseDynamicStringParts(parts, 0, sep, minSize));
                }
            }
        }
    }

    internal struct LRUCache<TKey, TValue> where TKey : IEquatable<TKey>
    {
        public struct Entry : IEquatable<Entry>
        {
            public LinkedListNode<TKey> lruNode;
            public TValue Value;
            public bool Equals(Entry other) => Value.Equals(other);
            public override int GetHashCode() => Value.GetHashCode();
        }
        int entryLimit;
        Dictionary<TKey, Entry> cache;
        LinkedList<TKey> lru;
        public LRUCache(int limit)
        {
            entryLimit = limit;
            cache = new Dictionary<TKey, Entry>();
            lru = new LinkedList<TKey>();
        }

        public bool TryAdd(TKey id, TValue obj)
        {
            if (obj == null || entryLimit <= 0)
                return false;

            cache.Add(id, new Entry { Value = obj, lruNode = lru.AddFirst(id) });

            while (lru.Count > entryLimit)
            {
                cache.Remove(lru.Last.Value);
                lru.RemoveLast();
            }
            return true;
        }


        public bool TryGet(TKey offset, out TValue val)
        {
            if (cache.TryGetValue(offset, out var entry))
            {
                val = entry.Value;
                if (entry.lruNode.Previous != null)
                {
                    lru.Remove(entry.lruNode);
                    lru.AddFirst(entry.lruNode);
                }
                return true;
            }
            val = default;
            return false;
        }
    }

}
