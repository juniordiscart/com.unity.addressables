using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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

            unsafe public object Deserialize(Reader reader, Type t, uint offset, out uint size)
            {
                size = 0;
                if (offset == uint.MaxValue)
                    return null;
                if (t == typeof(int))
                    return reader.ReadValue<int>(offset, out size);
                if (t == typeof(bool))
                    return reader.ReadValue<bool>(offset, out size);
                if (t == typeof(long))
                    return reader.ReadValue<long>(offset, out size);
                if (t == typeof(Hash128))
                    return reader.ReadValue<Hash128>(offset, out size);
                if (t == typeof(string))
                {
                    var remap = reader.ReadValue<ObjectToStringRemap>(offset, out size);
                    var str = reader.ReadString(remap.stringId, out var strSize, remap.separator, false);
                    size += strSize;
                    return str;
                }
                // Returning null for value types (e.g. typeof(double)) blew up later as an NRE.
                // Throwing here surfaces the missing adapter at the right call site.
                throw new NotSupportedException($"BuiltinTypesSerializer cannot deserialize type {t}; register a custom ISerializationAdapter<{t.Name}>.");
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

                if(bestCount == 0)
                    return (char)0;

                var parts = str.Split(bestSep);
                int validParts = 0;
                foreach (var p in parts)
                    if (p.Length > 4)
                        validParts++;

                return validParts > 1 ? bestSep : (char)0;
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
                    if(string.IsNullOrEmpty(str))
                        return uint.MaxValue;
                    var bestSep = FindBestSeparator(str, '/', '\\', '.', '-', '_', ',');
                    return writer.Write(new ObjectToStringRemap { stringId = writer.WriteString((string)val, bestSep), separator = bestSep });
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

            unsafe public object Deserialize(Reader reader, Type type, uint offset, out uint size)
            {
                try
                {
                    var d = reader.ReadValue<Data>(offset, out var dataSize);
                    var assemblyName = reader.ReadString(d.assemblyId, out var assemblySize, '.');
                    var className = reader.ReadString(d.classId, out var strSize, '.');
                    size = dataSize + assemblySize + strSize;
                    var assembly = Assembly.Load(assemblyName);
                    return assembly == null ? null : assembly.GetType(className);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    size = 0;
                    return null;
                }
            }

            public uint Serialize(Writer writer, object val)
            {
                if (val == null)
                    return uint.MaxValue;
                var t = val as Type;
                // Generic type definitions, dynamic assemblies and some constructed generics
                // can have null FullName. Refuse rather than write a null/poison entry that
                // would deserialize to garbage.
                var assemblyName = t.Assembly?.FullName;
                var className = t.FullName;
                if (assemblyName == null || className == null)
                    throw new NotSupportedException($"TypeSerializer cannot serialize type {t} (Assembly.FullName={assemblyName ?? "<null>"}, FullName={className ?? "<null>"}).");
                return writer.Write(new Data
                {
                    assemblyId = writer.WriteString(assemblyName, '.'),
                    classId = writer.WriteString(className, '.')
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
            AddSerializationAdapterImpl(serializationAdapters, adapter, forceOverride, new HashSet<ISerializationAdapter>());
        }

        static void AddSerializationAdapterImpl(Dictionary<Type, ISerializationAdapter> serializationAdapters, ISerializationAdapter adapter, bool forceOverride, HashSet<ISerializationAdapter> seen)
        {
            // Cycle guard: if Dependencies forms a cycle (A depends on B, B depends on A) the
            // prior implementation would loop forever once the dependency-skip bug was fixed.
            if (!seen.Add(adapter))
                return;

            foreach (var i in adapter.GetType().GetInterfaces())
            {
                if (i.IsGenericType && typeof(ISerializationAdapter).IsAssignableFrom(i))
                {
                    var aType = i.GenericTypeArguments[0];
                    if (serializationAdapters.TryGetValue(aType, out var existing))
                    {
                        if (forceOverride)
                        {
                            serializationAdapters[aType] = adapter;
                            Debug.Log($"Replacing adapter for type {aType}: {existing} -> {adapter}");
                        }
                        else
                        {
                            Debug.Log($"Failed to register adapter for type {aType}: {adapter}, {existing} is already registered.");
                        }
                    }
                    else
                    {
                        serializationAdapters[aType] = adapter;
                    }
                }
            }

            // Always register dependencies, even if the primary type slot was already taken.
            // The dependent adapters may handle types that nothing else covers, and skipping
            // them silently was the original bug.
            var deps = adapter.Dependencies;
            if (deps != null)
                foreach (var d in deps)
                    AddSerializationAdapterImpl(serializationAdapters, d, false, seen);
        }

        static bool GetSerializationAdapter(Dictionary<Type, ISerializationAdapter> serializationAdapters, Type t, out ISerializationAdapter adapter)
        {
            // Exact match first. Memoised lookups (including negative caching below) land here
            // and short-circuit before any reflection.
            if (serializationAdapters.TryGetValue(t, out adapter))
                return adapter != null;

            // Walk the base-class chain (most-derived to least). Avoids the prior implementation's
            // dependence on Dictionary iteration order, which is undefined and could pick a
            // different base adapter run-to-run.
            for (var current = t.BaseType; current != null; current = current.BaseType)
            {
                if (serializationAdapters.TryGetValue(current, out adapter))
                {
                    // Memoise: subsequent lookups of t skip the walk and the GetInterfaces alloc.
                    serializationAdapters[t] = adapter;
                    return adapter != null;
                }
            }

            // Then interfaces, in the order the runtime returns them (still deterministic per
            // type). GetInterfaces allocates an array — memoising the result avoids that on
            // subsequent reads of the same type.
            foreach (var i in t.GetInterfaces())
            {
                if (serializationAdapters.TryGetValue(i, out adapter))
                {
                    serializationAdapters[t] = adapter;
                    return adapter != null;
                }
            }

            Debug.LogError($"Unable to find serialization adapter for type {t}.");
            // Negative cache: future lookups of t hit the dictionary and return false without
            // re-walking. AddSerializationAdapter overwrites this entry if an adapter is later
            // registered for t.
            serializationAdapters[t] = null;
            adapter = null;
            return false;
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
            object Deserialize(Reader reader, Type t, uint offset, out uint size);
        }

        public interface ISerializationAdapter<T> : ISerializationAdapter
        {
        }

        // Private cache-key marker used by the non-generic ReadObjectArray(Type t,…) path.
        // The non-generic path returns object[] (not T[]), so it must not share a cache slot
        // with the generic ReadObjectArray<T>(id,…) path keyed by typeof(T[]). Without this
        // separation, a re-read in the generic path would attempt (T[])object[] and throw
        // InvalidCastException (reference types), or fail-silent via `as object[]` returning
        // null (value types).
        sealed class ObjectArrayCacheKey<T> { }

        public unsafe class Reader : IDisposable
        {
            byte[] m_Buffer;
            // Pin the buffer for the Reader's lifetime so each read uses a stored raw pointer
            // instead of paying the per-`fixed`-block setup cost. m_BufferHandle owns the pin;
            // Dispose (or the finalizer) frees it.
            GCHandle m_BufferHandle;
            byte* m_BufferPtr;
            bool m_Disposed;
            Dictionary<Type, ISerializationAdapter> m_Adapters;
            LRUCache<uint, object> m_Cache;
            uint m_MinCachedObjectSize = 0;
            // Per-Reader cache of element type → cache-key Type used by the non-generic
            // ReadObjectArray(Type t,…) path. The Type is `ObjectArrayCacheKey<elementType>`,
            // distinct from `elementType[]` so that the generic and non-generic array readers
            // never collide in the LRU cache. Memoised because MakeGenericType allocates a
            // fresh RuntimeType per call. Lazily initialised — Readers that never use the
            // non-generic path (e.g. the one-shot BinaryAssetProvider read) skip the
            // Dictionary allocation entirely.
            Dictionary<Type, Type> m_ObjectArrayKeyCache;
            public void GetCacheStats(out int reqCount, out int reqHits)
            {
                reqCount = m_Cache.requestCount;
                reqHits = m_Cache.requestHits;
            }

            public void ResetCache(int maxCachedObjects, uint minCachedObjSize)
            {
                m_MinCachedObjectSize = minCachedObjSize;
                m_Cache = new LRUCache<uint, object>(maxCachedObjects);
            }

            private void Init(byte[] data, int maxCachedObjects, uint minCachedObjSize, params ISerializationAdapter[] adapters)
            {
                m_Buffer = data;
                m_BufferHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                m_BufferPtr = (byte*)m_BufferHandle.AddrOfPinnedObject();
                m_MinCachedObjectSize = minCachedObjSize;
                m_Cache = new LRUCache<uint, object>(maxCachedObjects);
                m_Adapters = new Dictionary<Type, ISerializationAdapter>();
                // m_ObjectArrayKeyCache is lazy — see field comment.
                // Mirror Writer's ordering: built-ins first (no override), user adapters last
                // with forceOverride=true so their overrides and dependency chains take effect.
                BinaryStorageBuffer.AddSerializationAdapter(m_Adapters, new TypeSerializer());
                BinaryStorageBuffer.AddSerializationAdapter(m_Adapters, new BuiltinTypesSerializer());
                foreach (var a in adapters)
                    BinaryStorageBuffer.AddSerializationAdapter(m_Adapters, a, true);
            }

            public void AddSerializationAdapter(ISerializationAdapter a)
            {
                BinaryStorageBuffer.AddSerializationAdapter(m_Adapters, a);
            }

            public Reader(byte[] data, int maxCachedObjects = 1024, uint minCachedObjSize = 64, params ISerializationAdapter[] adapters)
            {
                Init(data, maxCachedObjects, minCachedObjSize, adapters);
            }

            // Release the GCHandle that pins the underlying byte[]. Calling Dispose is strongly
            // recommended — without it, the buffer stays pinned until the finalizer eventually
            // runs, which can cause heap fragmentation for small (SOH) buffers. Large catalog
            // buffers live on the LOH where pinning is essentially free, but the API contract
            // is the same regardless of size.
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~Reader()
            {
                // Finalizer safety net: free the pin so we don't leak it. Don't touch managed
                // state from here — only the unmanaged GCHandle is ours to release.
                Dispose(false);
            }

            void Dispose(bool disposing)
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                if (m_BufferHandle.IsAllocated)
                    m_BufferHandle.Free();
                m_BufferPtr = null;
                if (disposing)
                    m_Buffer = null;
            }

            internal byte[] GetBuffer()
            {
                return m_Buffer;
            }


            public Reader(Stream inputStream, uint bufferSize, int maxCachedObjects, uint minCachedObjSize, params ISerializationAdapter[] adapters)
            {
                var data = new byte[bufferSize == 0 ? inputStream.Length : bufferSize];
                int total = 0;
                while (total < data.Length)
                {
                    int n = inputStream.Read(data, total, data.Length - total);
                    if (n <= 0)
                        throw new EndOfStreamException($"Expected {data.Length} bytes from input stream, got {total}.");
                    total += n;
                }
                Init(data, maxCachedObjects, minCachedObjSize, adapters);
            }

            private bool TryGetCachedValue(Type t, uint offset, out object val, out uint size)
            {
                if (m_Cache.TryGet(t, offset, out var obj, out size))
                {
                    val = obj;
                    return true;
                }

                val = default;
                return false;
            }

            bool TryGetCachedValue<T>(uint offset, out T val, out uint size)
            {
                if (m_Cache.TryGet(typeof(T), offset, out var obj, out size))
                {
                    val = (T)obj;
                    return true;
                }

                val = default;
                return false;
            }

            public T[] ReadValueArray<T>(uint id, out uint readSize, bool cacheValue = true) where T : unmanaged
            {
                readSize = 0;
                if (id == uint.MaxValue)
                    return null;
                if (id - sizeof(uint) >= m_Buffer.Length)
                    throw new Exception($"Data offset {id} is out of bounds of buffer with length of {m_Buffer.Length}.");

                {
                    byte* pData = m_BufferPtr + (id - sizeof(uint));
                    if (TryGetCachedValue<T[]>(id, out var vals, out readSize))
                        return vals;
                    uint size = 0;
                    UnsafeUtility.MemCpy(&size, pData, sizeof(uint));
                    if ((id + size) > m_Buffer.Length)
                        throw new Exception($"Data size {size} is out of bounds of buffer with length of {m_Buffer.Length}.");
                    if (size % sizeof(T) != 0)
                        throw new Exception($"Data size {size} at offset {id} is not a multiple of sizeof({typeof(T)})={sizeof(T)}; data is truncated or the wrong type was requested.");
                    var elCount = size / sizeof(T);
                    var valsT = new T[elCount];
                    fixed (T* pVals = valsT)
                        UnsafeUtility.MemCpy(pVals, &pData[sizeof(uint)], size);
                    if (cacheValue && size >= m_MinCachedObjectSize)
                        m_Cache.TryAdd(id, typeof(T[]), valsT, size);
                    readSize = size;
                    return valsT;
                }
            }


            // Validates an object-array header at id (size-prefixed list of uint element ids)
            // and returns the element count. Centralised so every reader path shares the same
            // bounds checks instead of crashing with IndexOutOfRangeException on bad input.
            uint ValidateObjectArrayHeader(uint id)
            {
                if (id < sizeof(uint) || id > (uint)m_Buffer.Length)
                    throw new Exception($"Object array offset {id} is out of bounds of buffer with length of {m_Buffer.Length}.");
                uint sizeBytes = *(uint*)(m_BufferPtr + (id - sizeof(uint)));
                if (sizeBytes > (uint)m_Buffer.Length - id)
                    throw new Exception($"Object array at offset {id} (size {sizeBytes}) is out of bounds of buffer with length of {m_Buffer.Length}.");
                return sizeBytes / sizeof(uint);
            }

            public uint ProcessObjectArray<T, C>(uint id, out uint size, C context, Action<T, C, int, int> procFunc, bool cacheValues = true)
            {
                if (id == uint.MaxValue)
                {
                    size = 0;
                    return 0;
                }

                uint count = ValidateObjectArrayHeader(id);
                {
                    byte* pData = m_BufferPtr + (id - sizeof(uint));
                    uint totalSize = 0;
                    for (int i = 0; i < count; i++)
                    {
                        procFunc(ReadObject<T>(*((uint*)&pData[sizeof(uint) * (1 + i)]), out var objSize, cacheValues), context, i, (int)count);
                        totalSize += objSize;
                    }
                    size = totalSize;
                    return count;
                }
            }


            public uint ReadObjectArray<T>(ref List<T> results, uint id, out uint size, bool cacheValues = true)
            {
                if (id == uint.MaxValue)
                {
                    size = 0;
                    return 0;
                }

                uint count = ValidateObjectArrayHeader(id);
                {
                    byte* pData = m_BufferPtr + (id - sizeof(uint));
                    uint totalSize = 0;
                    for (int i = 0; i < count; i++)
                    {
                        results.Add(ReadObject<T>(*((uint*)&pData[sizeof(uint) * (1 + i)]), out var objSize, cacheValues));
                        totalSize += objSize;
                    }
                    size = totalSize;
                    return count;
                }
            }


            public object[] ReadObjectArray(uint id, out uint size, bool cacheValues = true, bool cacheFullArray = false)
            {
                if (id == uint.MaxValue)
                {
                    size = 0;
                    return null;
                }

                if (TryGetCachedValue<object[]>(id, out var res, out size))
                    return res;

                uint count = ValidateObjectArrayHeader(id);
                {
                    byte* pData = m_BufferPtr + (id - sizeof(uint));
                    uint totalSize = 0;
                    var objs = new object[count];
                    for (int i = 0; i < count; i++)
                    {
                        objs[i] = ReadObject(*((uint*)&pData[sizeof(uint) * (1 + i)]), out var objSize, cacheValues);
                        totalSize += objSize;
                    }
                    size = totalSize;
                    if (cacheFullArray && size >= m_MinCachedObjectSize)
                        m_Cache.TryAdd(id, typeof(object[]), objs, size);
                    return objs;
                }
            }

            public object[] ReadObjectArray(Type t, uint id, out uint size, bool cacheValues = true, bool cacheFullArray = false)
            {
                if (id == uint.MaxValue)
                {
                    size = 0;
                    return null;
                }
                // Key the cache on a marker type unique per element type and *distinct* from
                // typeof(t[]). Sharing typeof(t[]) with ReadObjectArray<T>(id,…) — which stores
                // a real T[] — would cause an InvalidCastException for reference types or a
                // silent null return for value types when the same id is read both ways.
                var keyCache = m_ObjectArrayKeyCache ??= new Dictionary<Type, Type>();
                if (!keyCache.TryGetValue(t, out var arrayKey))
                    keyCache[t] = arrayKey = typeof(ObjectArrayCacheKey<>).MakeGenericType(t);
                if (TryGetCachedValue(arrayKey, id, out var cached, out size))
                    return cached as object[];
                uint count = ValidateObjectArrayHeader(id);
                {
                    byte* pData = m_BufferPtr + (id - sizeof(uint));
                    uint totalSize = 0;
                    var objs = new object[count];
                    for (int i = 0; i < count; i++)
                    {
                        objs[i] = ReadObject(t, *((uint*)&pData[sizeof(uint) * (1 + i)]), out var objSize, cacheValues);
                        totalSize += objSize;
                    }
                    size = totalSize;
                    if (cacheFullArray && size >= m_MinCachedObjectSize)
                        m_Cache.TryAdd(id, arrayKey, objs, size);
                    return objs;
                }
            }

            public T[] ReadObjectArray<T>(uint id, out uint size, bool cacheValues = true, bool cacheFullArray = false)
            {
                if (id == uint.MaxValue)
                {
                    size = 0;
                    return null;
                }
                if (TryGetCachedValue<T[]>(id, out var res, out size))
                    return res;
                uint count = ValidateObjectArrayHeader(id);
                {
                    byte* pData = m_BufferPtr + (id - sizeof(uint));
                    uint totalSize = 0;
                    var objs = new T[count];
                    for (int i = 0; i < count; i++)
                    {
                        objs[i] = ReadObject<T>(*((uint*)&pData[sizeof(uint) * (1 + i)]), out var objSize, cacheValues);
                        totalSize += objSize;
                    }
                    size = totalSize;
                    if (cacheFullArray && size >= m_MinCachedObjectSize)
                        m_Cache.TryAdd(id, typeof(T[]), objs, size);
                    return objs;
                }
            }


            unsafe public object ReadObject(uint id, out uint size, bool cacheValue = true)
            {
                if (id == uint.MaxValue)
                {
                    size = 0;
                    return null;
                }
                // Cache the dispatched result under (id, typeof(object)) so repeated untyped
                // reads short-circuit before doing the ObjectTypeData read + Type lookup +
                // inner ReadObject(type,…) — three cache lookups collapse to one.
                if (TryGetCachedValue(typeof(object), id, out var cachedObj, out size))
                    return cachedObj;

                var td = ReadValue<ObjectTypeData>(id, out var tdSize);
                var type = ReadObject<Type>(td.typeId, out var typeSize);
                var obj = ReadObject(type, td.objectId, out var objSize, cacheValue);
                size = tdSize + typeSize + objSize;

                if (cacheValue && obj != null && size >= m_MinCachedObjectSize)
                    m_Cache.TryAdd(id, typeof(object), obj, size);
                return obj;
            }

            public T ReadObject<T>(uint id, out uint size, bool cacheValue = true)
            {
                size = 0;
                if (id == uint.MaxValue)
                    return default;

                if (TryGetCachedValue<T>(id, out var val, out size))
                    return val;

                if (!GetSerializationAdapter(m_Adapters, typeof(T), out var adapter))
                    return default;

                T res = default;
                try
                {
                    res = (T)adapter.Deserialize(this, typeof(T), id, out size);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return default;
                }

                if (cacheValue && res != null && size >= m_MinCachedObjectSize)
                    m_Cache.TryAdd(id, typeof(T), res, size);

                return res;
            }


            public object ReadObject(Type t, uint id, out uint size, bool cacheValue = true)
            {
                size = 0;
                if (id == uint.MaxValue)
                    return null;

                if (TryGetCachedValue(t, id, out var val, out size))
                    return val;

                if (!GetSerializationAdapter(m_Adapters, t, out var adapter))
                    return null;

                object res = default;
                try
                {
                    res = adapter.Deserialize(this, t, id, out size);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return null;
                }

                if (cacheValue && res != null && size >= m_MinCachedObjectSize)
                    m_Cache.TryAdd(id, t, res, size);

                return res;
            }

            public T ReadValue<T>(uint id, out uint size) where T : unmanaged
            {
                size = (uint)sizeof(T);
                if (id == uint.MaxValue)
                    return default;
                // Use subtraction to avoid uint overflow when id is near uint.MaxValue.
                if (id >= m_Buffer.Length || size > (uint)m_Buffer.Length - id)
                    throw new Exception($"Data offset {id} (size {size}) is out of bounds of buffer with length of {m_Buffer.Length}.");

                {
                    byte* pData = m_BufferPtr;
                    T val;
                    UnsafeUtility.MemCpy(&val, pData + id, size);
                    return val;
                }
            }

            public string ReadString(uint id, out uint size, char sep = (char)0, bool cacheValue = true)
            {
                if (id == uint.MaxValue)
                {
                    size = 0;
                    return null;
                }

                if (TryGetCachedValue<string>(id, out var str, out size))
                    return str;

                if (sep == (char)0)
                    return ReadAutoEncodedString(id, out size, cacheValue);
                return ReadDynamicString(id, out size, sep, cacheValue);
            }

            // cacheKey is the original id (still carrying the unicode flag); offset is the
            // masked buffer offset. Keying the cache by cacheKey keeps it consistent with the
            // outer ReadString cache check, which sees the flagged id.
            string ReadStringInternal(uint cacheKey, uint offset, out uint size, Encoding enc, bool cacheValue = true)
            {
                if (offset - sizeof(uint) >= m_Buffer.Length)
                    throw new Exception($"Data offset {offset} is out of bounds of buffer with length of {m_Buffer.Length}.");

                if (TryGetCachedValue<string>(cacheKey, out var val, out size))
                    return val;

                {
                    byte* pData = m_BufferPtr;
                    var strDataLength = *(uint*)&pData[offset - sizeof(uint)];
                    if (offset + strDataLength > m_Buffer.Length)
                        throw new Exception($"Data offset {offset}, len {strDataLength} is out of bounds of buffer with length of {m_Buffer.Length}.");

                    var valStr = enc.GetString(&pData[offset], (int)strDataLength);
                    size = strDataLength;
                    if (cacheValue && size >= m_MinCachedObjectSize)
                        m_Cache.TryAdd(cacheKey, typeof(string), valStr, size);
                    return valStr;
                }
            }

            string ReadAutoEncodedString(uint id, out uint size, bool cacheValue)
            {
                if ((id & kUnicodeStringFlag) == kUnicodeStringFlag)
                    return ReadStringInternal(id, (uint)(id & kClearFlagsMask), out size, Encoding.Unicode, cacheValue);
                return ReadStringInternal(id, id, out size, Encoding.ASCII, cacheValue);
            }

            public int ComputeStringLength(uint id, char sep = (char)0)
            {
                if (id == uint.MaxValue)
                    return 0;

                if (sep == (char)0)
                    return GetAutoEncodedStringLength(id);
                return GetDynamicStringLength(id, sep);
            }

            int GetDynamicStringLength(uint id, char sep)
            {
                if ((id & kDynamicStringFlag) == kDynamicStringFlag)
                {
                    int len = 0;
                    var nextID = id;
                    while (nextID != uint.MaxValue)
                    {
                        var ds = ReadValue<DynamicString>((uint)(nextID & kClearFlagsMask), out var _);
                        len += GetAutoEncodedStringLength(ds.stringId);
                        nextID = ds.nextId;
                        if (nextID != uint.MaxValue)
                            len++;
                    }
                    return len;
                }
                else
                {
                    return GetAutoEncodedStringLength(id);
                }
            }

            private int GetAutoEncodedStringLength(uint id)
            {
                if ((id & kUnicodeStringFlag) == kUnicodeStringFlag)
                    return GetStringLengthInternal((uint)(id & kClearFlagsMask), Encoding.Unicode);
                return GetStringLengthInternal(id, Encoding.ASCII);
            }

            private int GetStringLengthInternal(uint offset, Encoding enc)
            {
                if (offset - sizeof(uint) >= m_Buffer.Length)
                    throw new Exception($"Data offset {offset} is out of bounds of buffer with length of {m_Buffer.Length}.");

                {
                    byte* pData = m_BufferPtr;
                    var strDataLength = *(uint*)&pData[offset - sizeof(uint)];
                    if (offset + strDataLength > m_Buffer.Length)
                        throw new Exception($"Data offset {offset}, len {strDataLength} is out of bounds of buffer with length of {m_Buffer.Length}.");
                    return enc.GetCharCount(&pData[offset], (int)strDataLength);
                }
            }

            // Per-thread scratch list for accumulating decoded dynamic-string parts. Walking
            // the chain once into this list (instead of once for length + once for fill) saves
            // a duplicate pass of Encoding.GetCharCount + Dictionary lookups.
            [ThreadStatic]
            static List<string> t_dynamicStringParts;

#if UNITY_EDITOR
            // Drop any leftover string references from the previous playmode run so they don't
            // pin memory across domain reloads. Only the main thread's instance is reachable
            // here — worker threads handle their own lifetime.
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void ResetDynamicStringPartsOnLoad()
            {
                t_dynamicStringParts?.Clear();
            }
#endif

            // Static cached delegate so string.Create doesn't allocate a new SpanAction per
            // dynamic-string read. The state struct is passed by value, so the delegate has
            // no capture and can be a single shared instance.
            readonly struct DynamicStringFillState
            {
                public readonly List<string> parts;
                public readonly char sep;
                public DynamicStringFillState(List<string> parts, char sep) { this.parts = parts; this.sep = sep; }
            }
            static readonly System.Buffers.SpanAction<char, DynamicStringFillState> s_FillDynamicString = (chars, state) =>
            {
                // Parts were collected in chain-walk order, which is the *reverse* of the
                // original string's left-to-right order — the chain stores rightmost first.
                // Walking the list back-to-front rebuilds the original ordering.
                int pos = 0;
                var parts = state.parts;
                for (int i = parts.Count - 1; i >= 0; i--)
                {
                    var p = parts[i];
                    p.AsSpan().CopyTo(chars.Slice(pos));
                    pos += p.Length;
                    if (i > 0)
                        chars[pos++] = state.sep;
                }
            };

            string ReadDynamicString(uint id, out uint size, char sep, bool cacheValue)
            {
                if ((id & kDynamicStringFlag) != kDynamicStringFlag)
                    return ReadAutoEncodedString(id, out size, cacheValue);

                if (TryGetCachedValue<string>(id, out var str, out size))
                    return str;

                // Single-pass: walk the chain once, collecting decoded parts and totalling
                // both the rendered length and the underlying byte size. Parts are decoded
                // exactly once per cache miss (and zero times on subsequent cache hits at
                // the part level, since ReadAutoEncodedString caches above the size threshold).
                var parts = t_dynamicStringParts ??= new List<string>(8);
                try
                {
                    int totalLen = 0;
                    size = 0;
                    var nextID = id;
                    while (nextID != uint.MaxValue)
                    {
                        var ds = ReadValue<DynamicString>((uint)(nextID & kClearFlagsMask), out var dsSize);
                        var s = ReadAutoEncodedString(ds.stringId, out var strSize, true);
                        parts.Add(s);
                        totalLen += s.Length;
                        size += dsSize + strSize;
                        nextID = ds.nextId;
                        if (nextID != uint.MaxValue)
                            totalLen++; // separator character
                    }

                    str = string.Create(totalLen, new DynamicStringFillState(parts, sep), s_FillDynamicString);
                    if (cacheValue && size >= m_MinCachedObjectSize)
                        m_Cache.TryAdd(id, typeof(string), str, size);
                    return str;
                }
                finally
                {
                    // Release the per-thread part references promptly. Without this, the parts
                    // list pins them until the next dynamic-string read on this thread, which
                    // can be much later (or never) on a thread that reads infrequently. Clear
                    // releases the refs but keeps the list's capacity, so the next call still
                    // skips the underlying array allocation.
                    parts.Clear();
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
            uint defaultChunkSize;
            List<Chunk> chunks;
            Dictionary<Hash128, uint> existingValues;
            Dictionary<Type, ISerializationAdapter> serializationAdapters;
            public uint Length => totalBytes;

            public Writer(int chunkSize = 1024*1024, params ISerializationAdapter[] adapters)
            {
                defaultChunkSize = (uint)(chunkSize > 0 ? chunkSize : 1024 * 1024);
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
                    chunk.data = new byte[length > defaultChunkSize ? length : defaultChunkSize];

                if (length > chunk.data.Length - chunk.position)
                {
                    chunk = new Chunk { position = 0, data = new byte[length > defaultChunkSize ? length : defaultChunkSize] };
                    chunks.Add(chunk);
                }
                return chunk;
            }

            uint WriteInternal(void* pData, uint dataSize, bool prefixSize)
            {
                // Empty non-prefix writes have no storage to point at, so always sentinel them.
                // Doing this BEFORE the hash lookup also stops empty-data hashes (which all
                // collapse to default(Hash128)) from aliasing onto a previously-written
                // prefix-path empty entry.
                if (dataSize == 0 && !prefixSize)
                    return uint.MaxValue;

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
                        UnsafeUtility.MemCpy(pChunkData, pData, dataSize);
                    }

                    totalBytes += addedBytes;
                    chunk.position += addedBytes;
                    EnsureWithinAddressableLimit();
                    existingValues[hash] = id;
                    return id;
                }
            }

            // The top two bits of every offset are reserved for kUnicodeStringFlag /
            // kDynamicStringFlag, so writable offsets must fit into kClearFlagsMask (~1 GB).
            // Failing fast here is far easier to diagnose than the silent flag-bit corruption
            // a too-large offset would otherwise produce in encoded ids.
            void EnsureWithinAddressableLimit()
            {
                if (totalBytes > kClearFlagsMask)
                    throw new InvalidOperationException($"BinaryStorageBuffer.Writer exceeded the {kClearFlagsMask}-byte (~1 GB) addressable limit; offsets share their top two bits with string flags and cannot grow further.");
            }

            uint ReserveInternal(uint dataSize, bool prefixSize)
            {
                //reserved data cannot reuse previously hashed values, but it can be reused for future writes
                var addedBytes = prefixSize ? dataSize + sizeof(uint) : dataSize;
                var chunk = FindChunkWithSpace(addedBytes);


                totalBytes += addedBytes;
                chunk.position += addedBytes;
                EnsureWithinAddressableLimit();
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
                        EnsureWithinAddressableLimit();
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
                    // Match Write<T>(T[]): record the array's hash so a future identical array
                    // write deduplicates. The reservation-write path previously skipped this.
                    Hash128 arrayHash;
                    ComputeHash(pValues, (ulong)dataSize, &arrayHash);

                    foreach (var c in chunks)
                    {
                        if (chunkOffset < c.position)
                        {
                            fixed (byte* pChunkData = c.data)
                            {
                                UnsafeUtility.MemCpy(&pChunkData[chunkOffset - sizeof(uint)], &dataSize, sizeof(uint));
                                UnsafeUtility.MemCpy(&pChunkData[chunkOffset], pValues, dataSize);

                                existingValues[arrayHash] = offset;

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
                // The reservation must have been issued from this writer; if we can't find it
                // the caller passed a bogus offset (or chunks were rebuilt). Fail loudly
                // instead of returning a sentinel that callers might silently propagate.
                throw new ArgumentOutOfRangeException(nameof(offset), $"Reservation offset {offset} not found in any chunk.");
            }

            public uint WriteObjects<T>(IEnumerable<T> objs, bool serializeTypeData)
            {
                if (objs == null)
                    return uint.MaxValue;
                // Materialise once: passing a non-replayable IEnumerable (an iterator block)
                // previously did Count() + foreach, with the foreach yielding nothing on a
                // single-pass enumerable.
                var materialised = objs as ICollection<T> ?? objs.ToList();
                var ids = new uint[materialised.Count];
                var index = 0;
                foreach (var o in materialised)
                    ids[index++] = WriteObject(o, serializeTypeData);
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
                    if (str[i] > 127)
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
                var split = str.Split(sep);
                var minSize = (uint)sizeof(DynamicString);
                var parts = new StringParts[split.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    var partSize = ComputeStringSize(split[i], out var isUnicode);
                    parts[i] = new StringParts { str = split[i], dataSize = partSize, isUnicode = isUnicode };
                }

                // Include the separator byte the merge would add, so the threshold matches the
                // accounting in BuildDynamicStringChain (which adds +1 for the separator).
                if (parts.Length < 2 || (parts.Length == 2 && (parts[0].dataSize + parts[1].dataSize + 1) < minSize))
                {
                    return WriteAutoEncodedString(str);
                }
                else
                {
                    return (kDynamicStringFlag | BuildDynamicStringChain(parts, parts.Length - 1, sep, minSize));
                }
            }

            // Iterative replacement for the previously-recursive RecurseDynamicStringParts.
            // Pathological inputs (many small unmergeable parts) could otherwise stack-overflow.
            uint BuildDynamicStringChain(StringParts[] parts, int initialIndex, char sep, uint minSize)
            {
                // First pass: find the "head" index for each chain link by walking forward and
                // merging small tail parts into their predecessor (same logic as the original).
                var levelHeads = new List<int>();
                int index = initialIndex;
                while (index >= 0)
                {
                    while (index > 0)
                    {
                        var currPartSize = parts[index].dataSize;
                        if (currPartSize >= minSize)
                            break;
                        parts[index - 1].str = $"{parts[index - 1].str}{sep}{parts[index].str}";
                        parts[index - 1].dataSize += currPartSize + 1;
                        parts[index - 1].isUnicode |= parts[index].isUnicode;
                        index--;
                    }
                    levelHeads.Add(index);
                    index--;
                }

                // Second pass: build the chain tail-first so each link's nextId is known when we
                // write it. The final write is the chain head; return its id.
                uint nextId = uint.MaxValue;
                for (int i = levelHeads.Count - 1; i >= 0; i--)
                {
                    var k = levelHeads[i];
                    var strId = parts[k].isUnicode ? WriteUnicodeString(parts[k].str) : WriteStringInternal(parts[k].str, Encoding.ASCII);
                    nextId = Write(new DynamicString { stringId = strId, nextId = nextId });
                }
                return nextId;
            }
        }
    }

    // Class (not struct) so that mutation of counters remains consistent regardless of how
    // a reference is passed around. The dictionary/list inside are reference types, so a
    // struct copy would silently share state with the original — that fragility is gone now.
    internal class LRUCache<TKey, TValue> where TKey : IEquatable<TKey>
    {
        public struct Key : IEquatable<Key>
        {
            static readonly Type typeType = typeof(Type);
            public TKey key;
            public Type type;
            public Key(TKey k, Type t)
            {
                key = k;
                type = t;
                if (type != null && typeType.IsAssignableFrom(type))
                    type = typeType;
            }
            public bool Equals(Key other) => key.Equals(other.key) && type == other.type;
            public override bool Equals(object obj) => obj is Key k && Equals(k);
            public override int GetHashCode() => key.GetHashCode() ^ type.GetHashCode();
        }

        // Intrusive doubly-linked list: prev/next live on the Entry itself, so promotion and
        // eviction don't allocate any auxiliary node objects. When the cache is full, the
        // evicted Entry instance is reused for the new insert — at steady state, an inserting
        // cache only allocates Dictionary internal slots, no Entry/LinkedListNode objects.
        sealed class Entry
        {
            public Key key;
            public TValue Value;
            // Size of the original deserialised payload, returned to callers on cache hit so
            // that parent adapters that accumulate child sizes get accurate totals (and in turn
            // make accurate caching decisions for themselves).
            public uint Size;
            public Entry prev;
            public Entry next;
        }

        public int requestHits;
        public int requestCount;
        int entryLimit;
        Dictionary<Key, Entry> cache;
        Entry head;   // MRU end — most-recently used
        Entry tail;   // LRU end — eviction candidate

        public LRUCache(int limit)
        {
            requestHits = requestCount = 0;
            entryLimit = limit;
            cache = new Dictionary<Key, Entry>(limit);
        }

        void Unlink(Entry e)
        {
            if (e.prev != null) e.prev.next = e.next; else head = e.next;
            if (e.next != null) e.next.prev = e.prev; else tail = e.prev;
            e.prev = e.next = null;
        }

        void LinkAtHead(Entry e)
        {
            e.prev = null;
            e.next = head;
            if (head != null) head.prev = e;
            head = e;
            if (tail == null) tail = e;
        }

        // Add by the caller-specified type so that polymorphic readers (e.g. ReadObject<T>
        // returning a derived instance) can be retrieved with the same lookup key they used.
        public bool TryAdd(TKey id, Type type, TValue obj, uint size)
        {
            if (obj == null || entryLimit <= 0)
                return false;
            var key = new Key(id, type);
            if (cache.TryGetValue(key, out var existing))
            {
                // Already present — promote to MRU instead of failing silently.
                if (existing != head)
                {
                    Unlink(existing);
                    LinkAtHead(existing);
                }
                return false;
            }

            Entry entry;
            if (cache.Count >= entryLimit)
            {
                // Reuse the LRU Entry instead of allocating a fresh one. This is the steady-state
                // path once the cache fills — keeps insert allocation cost limited to the
                // dictionary slot itself.
                entry = tail;
                cache.Remove(entry.key);
                Unlink(entry);
                entry.key = key;
                entry.Value = obj;
                entry.Size = size;
            }
            else
            {
                entry = new Entry { key = key, Value = obj, Size = size };
            }
            LinkAtHead(entry);
            cache.Add(key, entry);
            return true;
        }

        public bool TryGet(Type type, TKey id, out TValue val, out uint size)
        {
            RecordRequest();
            var key = new Key(id, type);
            if (cache.TryGetValue(key, out var entry))
            {
                val = entry.Value;
                size = entry.Size;
                // Promote to MRU.
                if (entry != head)
                {
                    Unlink(entry);
                    LinkAtHead(entry);
                }
                RecordHit();
                return true;
            }
            val = default;
            size = 0;
            return false;
        }

        // Stat counters compile out unless BINARY_STORAGE_BUFFER_STATS is defined. The two
        // increments per TryGet were always-on overhead and cache-line write traffic; gating
        // them keeps GetCacheStats available for opt-in profiling without paying in production.
        [System.Diagnostics.Conditional("BINARY_STORAGE_BUFFER_STATS")]
        void RecordRequest() => requestCount++;
        [System.Diagnostics.Conditional("BINARY_STORAGE_BUFFER_STATS")]
        void RecordHit() => requestHits++;
    }

}
