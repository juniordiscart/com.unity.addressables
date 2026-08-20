using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NUnit.Framework;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using Debug = UnityEngine.Debug;

namespace UnityEditor.AddressableAssets.Tests
{
    public class BinaryStorageBufferTests
    {
        [StructLayout(LayoutKind.Sequential)]
        struct SimpleStruct : IEquatable<SimpleStruct>
        {
            public int intVal;
            public float floatVal;
            public byte byteVal;
            public short shortVal;
            public long longVal;
            public char charVal;
            public SimpleStruct(int multiple)
            {
                intVal = 8 * multiple;
                floatVal = 3.14f * multiple;
                byteVal = (byte)(16 * multiple);
                shortVal = (short)(125 * multiple);
                longVal = 10000000 * multiple;
                charVal = 'u';
            }

            public bool Equals(SimpleStruct other)
            {
                return intVal.Equals(other.intVal) && floatVal.Equals(other.floatVal) && byteVal.Equals(other.byteVal) && shortVal.Equals(other.shortVal) && longVal.Equals(other.longVal) && charVal.Equals(other.charVal);
            }
        }

        [Test]
        public void TestValueTypes([Values(1024, 1024 * 1024)]int chunkSize, [Values(0, 10, 1024)]int cacheSize)
        {
            var wr = new BinaryStorageBuffer.Writer(chunkSize);
            var intID = wr.Write(5);
            var floatId = wr.Write(3.14f);
            var boolId = wr.Write(true);
            var structId1 = wr.Write(new SimpleStruct(1));
            var structId2 = wr.Write(new SimpleStruct(2));

            var bytes = wr.SerializeToByteArray();
            var re = new BinaryStorageBuffer.Reader(bytes, cacheSize);
            Assert.AreEqual(5, re.ReadValue<int>(intID, out var _));
            Assert.AreEqual(true, re.ReadValue<bool>(boolId, out var _));
            Assert.AreEqual(new SimpleStruct(1), re.ReadValue<SimpleStruct>(structId1, out var _));
            Assert.AreEqual(new SimpleStruct(2), re.ReadValue<SimpleStruct>(structId2, out var _));
        }

        [Test]
        public void TestValueTypesWithReserve([Values(1024, 1024 * 1024)]int chunkSize, [Values(0, 10, 1024)]int cacheSize)
        {
            var wr = new BinaryStorageBuffer.Writer(chunkSize);
            var intID = wr.Write(wr.Reserve<int>(), 5);
            var floatId = wr.Write(wr.Reserve<float>(), 3.14f);
            var boolId = wr.Write(wr.Reserve<bool>(), true);
            var structId1 = wr.Write(wr.Reserve<SimpleStruct>(), new SimpleStruct(1));
            var structId2 = wr.Write(wr.Reserve<SimpleStruct>(), new SimpleStruct(2));

            var bytes = wr.SerializeToByteArray();
            var re = new BinaryStorageBuffer.Reader(bytes, cacheSize);
            Assert.AreEqual(5, re.ReadValue<int>(intID, out var _));
            Assert.AreEqual(true, re.ReadValue<bool>(boolId, out var _));
            Assert.AreEqual(new SimpleStruct(1), re.ReadValue<SimpleStruct>(structId1, out var _));
            Assert.AreEqual(new SimpleStruct(2), re.ReadValue<SimpleStruct>(structId2, out var _));
        }

        [Test]
        public void TestValueTypesWithUnorderedReserve([Values(1024, 1024 * 1024)]int chunkSize, [Values(0, 10, 1024)]int cacheSize)
        {
            var wr = new BinaryStorageBuffer.Writer(chunkSize);
            var intID = wr.Reserve<int>();
            var floatId = wr.Reserve<float>();
            var boolId = wr.Write(wr.Reserve<bool>(), true);
            var structId1 = wr.Reserve<SimpleStruct>();
            var structId2 = wr.Write(wr.Reserve<SimpleStruct>(), new SimpleStruct(2));
            wr.Write(floatId, 3.14f);
            wr.Write(intID, 5);
            wr.Write(structId1, new SimpleStruct(1));
            wr.Write(structId1, new SimpleStruct(1));
            var bytes = wr.SerializeToByteArray();
            var re = new BinaryStorageBuffer.Reader(bytes, cacheSize);
            Assert.AreEqual(5, re.ReadValue<int>(intID, out var _));
            Assert.AreEqual(true, re.ReadValue<bool>(boolId, out var _));
            Assert.AreEqual(new SimpleStruct(1), re.ReadValue<SimpleStruct>(structId1, out var _));
            Assert.AreEqual(new SimpleStruct(2), re.ReadValue<SimpleStruct>(structId2, out var _));
        }

        [Test]
        public void TestValueTypeArrays([Values(1024, 1024 * 1024)]int chunkSize, [Values(0, 1, 32, 256, 1024)]int count, [Values(0, 10, 1024)]int cacheSize)
        {
            var array = new SimpleStruct[count];
            var array2 = new SimpleStruct[count];

            for (int i = 0; i < array.Length; i++)
            {
                array2[i] = new SimpleStruct(array2.Length - i);
                array[i] = new SimpleStruct(i);
            }

            var wr = new BinaryStorageBuffer.Writer(chunkSize);
            var arrayId2 = wr.Reserve<SimpleStruct>((uint)count);
            var arrayId = wr.Write(array);
            wr.Write(arrayId2, array2);

            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize);
            var a1 = re.ReadValueArray<SimpleStruct>(arrayId, out var _);
            var a2 = re.ReadValueArray<SimpleStruct>(arrayId2, out var _);
            for (int i = 0; i < array.Length; i++)
            {
                Assert.AreEqual(array[i], a1[i]);
                Assert.AreEqual(array2[i], a2[i]);
            }
        }

        [Test]
        public void TestValueTypeDeduplication([Values(1024, 1024 * 1024)]int chunkSize)
        {
            var wr = new BinaryStorageBuffer.Writer(chunkSize);
            wr.Write(new SimpleStruct(1));
            wr.Write(new SimpleStruct(2));
            wr.Write(new SimpleStruct(3));
            wr.Write(new SimpleStruct(4));
            wr.Write(new SimpleStruct(5));
            var size = wr.Length;
            wr.Write(new SimpleStruct(5));
            wr.Write(new SimpleStruct(4));
            wr.Write(new SimpleStruct(3));
            wr.Write(new SimpleStruct(2));
            wr.Write(new SimpleStruct(1));
            Assert.AreEqual(size, wr.Length);
        }
        [Test]
        public void TestValueArrayTypeDeduplication([Values(1024, 1024 * 1024)]int chunkSize, [Values(0, 1, 32, 256, 1024)]int count)
        {
            var array = new SimpleStruct[count];
            for (int i = 0; i < array.Length; i++)
                array[i] = new SimpleStruct(i);

            var wr = new BinaryStorageBuffer.Writer(chunkSize);
            wr.Write(array);
            var size = wr.Length;
            for (int i = 0; i < array.Length; i++)
                wr.Write(array[i]);
            Assert.AreEqual(size, wr.Length);
        }

        const string ucSample = "Ё Ђ Ѓ Є Ѕ І Ї Ј Љ Њ Ћ Ќ Ў Џ А Б В Г Д Е Ж З И Й К Л М Н О П Р С Т У Ф Х Ц Ч Ш Щ Ъ Ы Ь Э Ю Я а б в г д е ж з и й к л м н о п р с т у ф х ц ч ш щ ъ ы ь э ю я ё ђ ѓ є ѕ і ї ј љ њ ћ ќ ў џ Ѡ ѡ Ѣ ѣ Ѥ ѥ Ѧ ѧ Ѩ ѩ Ѫ ѫ Ѭ ѭ Ѯ ѯ Ѱ ѱ Ѳ ѳ Ѵ ѵ Ѷ ѷ Ѹ ѹ Ѻ ѻ Ѽ ѽ Ѿ ѿ Ҁ ҁ ҂ ҃ ...";
        string RandomText(int len, bool unicode, char sep)
        {
            var sb = new StringBuilder(len);
            var appendCount = 0;
            if (unicode)
            {
                for (int i = 0; i < len; i++)
                {
                    if (appendCount++ > UnityEngine.Random.Range(10, 30))
                    {
                        sb.Append(sep);
                        appendCount = 0;
                    }
                    else
                        sb.Append(ucSample[UnityEngine.Random.Range(0, ucSample.Length)]);
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    if (appendCount++ > UnityEngine.Random.Range(10, 30))
                    {
                        sb.Append(sep);
                        appendCount = 0;
                    }
                    else
                        sb.Append((char)UnityEngine.Random.Range((int)'a', (int)'z'));
                }
            }
            return sb.ToString();
        }

        [Test]
        public void TestDynamicStringsReturnCachedValue()
        {
            var str = "text/with/lots/of/slahes";
            var wr = new BinaryStorageBuffer.Writer(1024);
            var strId1 = wr.WriteString(str, '/');
            var strId2 = wr.WriteString(str, '/');
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), 1024, 0);
            var str1 = re.ReadString(strId1, out var str1Size, '/');
            var str2 = re.ReadString(strId2, out var str2Size, '/');
            Assert.AreEqual(46, str1Size);
            Assert.AreEqual(46, str2Size);
            Assert.AreSame(str1, str2);
        }

        [Test]
        public void TestStringAsObject()
        {
            var txt = RandomText(1000, false, '/');
            var objTxt = txt as object;
            var wr = new BinaryStorageBuffer.Writer(1024, new ComplexObject.Serializer());
            var headerOffset = wr.Reserve<DateTime>();
            var reserve2 = wr.Reserve<DateTime>(100000);
            wr.Write(new int[100000]);
            for (int i = 0; i < 100000; i++)
                wr.WriteString(txt);
                //wr.WriteObject(new ComplexObject(i), true);
            var str = wr.WriteObject(objTxt, true);
            var bytes = wr.SerializeToByteArray();
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), 1024);
            var rStr = re.ReadObject(str, out var _) as string;
           //var rStr = re.ReadString(
            Assert.AreEqual(txt, rStr);
        }

        [Test]
        public void TestASCIIStrings([Values(1024, 1024 * 1024)]int chunkSize, [Values(0, 1, 10, 100, 1000, 5000)]int strLen, [Values(0, 10, 1024)]int cacheSize)
        {
            var txt = RandomText(strLen, false, '/');
            var sep = (char)UnityEngine.Random.Range((int)'a', (int)'z');
            var wr = new BinaryStorageBuffer.Writer(chunkSize);
            var str = wr.WriteString(txt, sep);
            var bytes = wr.SerializeToByteArray();
            var re = new BinaryStorageBuffer.Reader(bytes, cacheSize);
            var strRes = re.ReadString(str, out var _, sep);
            Assert.AreEqual(txt, strRes);
        }
        [Test]
        public void TestASCIIStringsDeduplication([Values(1024, 1024 * 1024)]int chunkSize, [Values(0, 1, 10, 100, 1000, 5000)]int strLen, [Values(0, 10, 1024)]int cacheSize)
        {
            var txt = RandomText(strLen, false, '/');
            var sep = (char)UnityEngine.Random.Range((int)'a', (int)'z');
            var wr = new BinaryStorageBuffer.Writer(chunkSize);
            var str = wr.WriteString(txt, sep);
            var size = wr.Length;
            var str2 = wr.WriteString(txt, sep);
            Assert.AreEqual(size, wr.Length);
            Assert.AreEqual(str, str2);

            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize);
            Assert.AreEqual(txt, re.ReadString(str, out var _, sep));
            Assert.AreEqual(txt, re.ReadString(str2, out var _, sep));
        }

        [Test]
        public void TestUnicodeStrings([Values(1024, 1024 * 1024)]int chunkSize, [Values(0, 1, 10, 100, 1000, 5000)]int strLen, [Values(0, 10, 1024)]int cacheSize)
        {
            var txt = RandomText(strLen, true, '/');
            var sep = (char)UnityEngine.Random.Range((int)'a', (int)'z');
            var wr = new BinaryStorageBuffer.Writer(chunkSize);
            var str = wr.WriteString(txt, sep);
            var bytes = wr.SerializeToByteArray();
            var re = new BinaryStorageBuffer.Reader(bytes, cacheSize);
            var strRes = re.ReadString(str, out var _, sep);
            Assert.AreEqual(txt, strRes);
        }

        [Test]
        public void TestStringExamples(
            [Values(8, 256, 1024)]int chunkSize,
            [Values(
            null,
            "",
            "1",
            "string",
            "a/b/c/d/f/g/h/i/j/k",
            "rootfolder1/rootfolder2/rootfolder3/long file name",
            "rootfolder1/rootЁfolder2/rootfolder3_withЁ/long file name",
            "a/b/wergwegbwethgrwtherth/c/e/ffdsfsrgwetghwthwrh/e/s/wergwethgwrthrewthwer",
            "a/b/wergwegbwethgrwtherth/Ёc/e/ffdsfsЁrgwetghwthwrh/e/s/wergwethgwrthrewthweЁr",
            "å",
            "Åland",
            "folder/Rådata/file.asset"
            )]string str, [Values(0, 10, 1024)]int cacheSize)
        {
            var wr = new BinaryStorageBuffer.Writer(chunkSize);
            var id = wr.WriteString(str, '/');
            var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize);
            var str2 = r.ReadString(id, out var _, '/');
            Assert.AreEqual(str, str2);
        }

        [Test]
        [TestCase(
    "Assets/Test/Folder/a/b/c/d/123234.json",
    "Assets/Test/Folder/a/b/c/d/123235.json",
    "Assets/Test/Folder/a/b/c/d/123236.json",
    "Assets/Test/Folder/a/b/c/d/123237.json",
    "Assets/Test/Folder/a/b/c/d/123238.json",
    "Assets/Test/Folder/a/b/c/d/123239.json",
    "Assets/Test/Folder/a/b/c/d/123230.json",
    "Assets/Test/Folder/a/b/c/d/123240.json",
    "Assets/Test/Folder/a/b/c/d/123241.json",
    "Assets/Test/Folder/a/b/c/d/123242.json",
    "Assets/Test/Folder/a/b/c/d/123243.json",
    TestName = "StringDeduplication_Common_Prefixes")]
        public void TestStringDeduplication(params string[] strs)
        {
            int rawSize = 0;
            var wr = new BinaryStorageBuffer.Writer(256);
            var ids = new List<uint>();
            foreach (var s in strs)
            {
                rawSize += s.Length;
                ids.Add(wr.WriteString(s, '/'));
            }
            var data = wr.SerializeToByteArray();
            var br = new BinaryStorageBuffer.Reader(data);

            for (int i = 0; i < ids.Count; i++)
                Assert.AreEqual(strs[i], br.ReadString(ids[i], out var _, '/'));

            Assert.Less(data.Length, rawSize);
        }

        [Test]
        public void PerfTestStringExamples()
        {
            var wr = new BinaryStorageBuffer.Writer(1024);
            var unicodeStr = "unicode string = Ё";
            var asciiStr = "ascii string";

            int count = 10000;
            var ids = new uint[count];
            for (int i = 0; i < count; i++)
            {
                var sb = new StringBuilder(1000);
                var partCount = UnityEngine.Random.Range(2, 10);
                for (int j = 0; j < partCount; j++)
                {
                    var v = UnityEngine.Random.Range(0, 10);
                    if (v > 4)
                        sb.Append($"{asciiStr} - {v}");
                    else
                        sb.Append($"{unicodeStr} - {v}");
                    sb.Append('/');
                }

                ids[i] = wr.WriteString(sb.ToString(), '/');
            }

            var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), 1024);
            MeasureReads("ReadString", 100000, () =>
                r.ReadString(ids[UnityEngine.Random.Range(0, ids.Length)], out var _, '/'));
        }

        // Helper that runs a measured loop, doing a small warm-up pass first so JIT compile
        // costs don't dominate, then reporting wall time and (where available) allocated bytes.
        // Allocation tracking uses GC.GetAllocatedBytesForCurrentThread; not all platforms /
        // Mono builds support it, so failures are caught and the metric is omitted.
        static void MeasureReads(string label, int iterations, Action body)
        {
            // Warm up.
            for (int i = 0; i < Math.Min(1000, iterations / 10); i++)
                body();

            long allocBefore = -1;
            try { allocBefore = GC.GetAllocatedBytesForCurrentThread(); }
            catch { /* unsupported on this runtime; we'll skip the alloc figure */ }

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                body();
            sw.Stop();

            var ms = sw.Elapsed.TotalMilliseconds;
            var nsPerOp = (sw.Elapsed.TotalMilliseconds * 1_000_000.0) / iterations;
            string allocStr = "n/a";
            if (allocBefore >= 0)
            {
                try
                {
                    long allocAfter = GC.GetAllocatedBytesForCurrentThread();
                    var bytesPerOp = (allocAfter - allocBefore) / (double)iterations;
                    allocStr = $"{bytesPerOp:F1} B/op";
                }
                catch { }
            }
            Debug.Log($"{label}: {ms:F1}ms total ({iterations:N0} ops, {nsPerOp:F0} ns/op, {allocStr})");
        }

        // Random value-type reads. Exercises ReadValue<T> (no per-call allocation expected).
        [Test]
        public void PerfTestReadValue()
        {
            const int count = 10000;
            var wr = new BinaryStorageBuffer.Writer(1024 * 1024);
            var ids = new uint[count];
            for (int i = 0; i < count; i++)
                ids[i] = wr.Write(new SimpleStruct(i + 1));

            using var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), 0);
            MeasureReads("ReadValue<SimpleStruct>", 200000, () =>
                r.ReadValue<SimpleStruct>(ids[UnityEngine.Random.Range(0, ids.Length)], out var _));
        }

        // Random typed value-array reads. First read of each id allocates a T[]; subsequent
        // reads of the same id should hit the cache and return the cached array (zero alloc).
        [Test]
        public void PerfTestReadValueArray()
        {
            const int arrayCount = 1000;
            const int elementsPerArray = 32;
            var wr = new BinaryStorageBuffer.Writer(1024 * 1024);
            var ids = new uint[arrayCount];
            for (int i = 0; i < arrayCount; i++)
            {
                var arr = new SimpleStruct[elementsPerArray];
                for (int j = 0; j < elementsPerArray; j++)
                    arr[j] = new SimpleStruct(i * 1000 + j);
                ids[i] = wr.Write(arr);
            }

            // minCachedObjSize = 0 → every array gets cached on first read.
            using var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), arrayCount * 2, 0);
            MeasureReads("ReadValueArray<SimpleStruct>", 100000, () =>
                r.ReadValueArray<SimpleStruct>(ids[UnityEngine.Random.Range(0, ids.Length)], out var _));
        }

        // Random typed object reads through an adapter. With the cache warmed every read is a
        // cache hit, so this measures the dispatch + cache-lookup hot path.
        [Test]
        public void PerfTestReadObjectTyped()
        {
            const int count = 5000;
            var wr = new BinaryStorageBuffer.Writer(1024 * 1024, new ComplexObject.Serializer());
            var ids = new uint[count];
            for (int i = 0; i < count; i++)
                ids[i] = wr.WriteObject(new ComplexObject(i), false);

            // minCachedObjSize = 0 ensures the cache fills even for tiny adapter-reported sizes.
            using var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), count * 2, 0, new ComplexObject.Serializer());
            MeasureReads("ReadObject<ComplexObject>", 200000, () =>
                r.ReadObject<ComplexObject>(ids[UnityEngine.Random.Range(0, ids.Length)], out var _));
        }

        // Same data but read untyped — exercises the outer ReadObject(id) path that pins
        // Fix-#6 (outer-id cache short-circuit). With it, a steady-state read is one cache
        // lookup; without it, three.
        [Test]
        public void PerfTestReadObjectUntyped()
        {
            const int count = 5000;
            var wr = new BinaryStorageBuffer.Writer(1024 * 1024, new ComplexObject.Serializer());
            var ids = new uint[count];
            for (int i = 0; i < count; i++)
                ids[i] = wr.WriteObject(new ComplexObject(i), true); // serializeTypeData=true → outer id

            using var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), count * 2, 0, new ComplexObject.Serializer());
            MeasureReads("ReadObject(id) untyped", 200000, () =>
                r.ReadObject(ids[UnityEngine.Random.Range(0, ids.Length)], out var _));
        }

        // Random typed object-array reads. cacheFullArray=true so subsequent reads of the
        // same id hit the array cache.
        [Test]
        public void PerfTestReadObjectArrayTyped()
        {
            const int arrayCount = 200;
            const int elementsPerArray = 16;
            var wr = new BinaryStorageBuffer.Writer(1024 * 1024, new ComplexObject.Serializer());
            var ids = new uint[arrayCount];
            for (int i = 0; i < arrayCount; i++)
            {
                var arr = new ComplexObject[elementsPerArray];
                for (int j = 0; j < elementsPerArray; j++)
                    arr[j] = new ComplexObject(i * 1000 + j);
                ids[i] = wr.WriteObjects(arr, false);
            }

            using var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), arrayCount * 2, 0, new ComplexObject.Serializer());
            MeasureReads("ReadObjectArray<ComplexObject>", 50000, () =>
                r.ReadObjectArray<ComplexObject>(ids[UnityEngine.Random.Range(0, ids.Length)], out var _, true, true));
        }

        // Random untyped object-array reads via ReadObjectArray(Type t,…). Tests the
        // MakeArrayType-memoisation hot path.
        [Test]
        public void PerfTestReadObjectArrayByType()
        {
            const int arrayCount = 200;
            const int elementsPerArray = 16;
            var wr = new BinaryStorageBuffer.Writer(1024 * 1024, new ComplexObject.Serializer());
            var ids = new uint[arrayCount];
            for (int i = 0; i < arrayCount; i++)
            {
                var arr = new ComplexObject[elementsPerArray];
                for (int j = 0; j < elementsPerArray; j++)
                    arr[j] = new ComplexObject(i * 1000 + j);
                ids[i] = wr.WriteObjects(arr, false);
            }

            using var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), arrayCount * 2, 0, new ComplexObject.Serializer());
            MeasureReads("ReadObjectArray(typeof(ComplexObject))", 50000, () =>
                r.ReadObjectArray(typeof(ComplexObject), ids[UnityEngine.Random.Range(0, ids.Length)], out var _, true, true));
        }



        class ComplexObject : IEquatable<ComplexObject>
        {
            public class ComplexSubClass : IEquatable<ComplexSubClass>
            {
                public string stringVal;
                public float floatV;
                public bool Equals(ComplexSubClass other) => stringVal.Equals(other.stringVal) && floatV.Equals(other.floatV);
            }
            public int intVal;
            public string stringVal;
            public ComplexSubClass sub;
            public ComplexObject() { }
            public ComplexObject(int seed)
            {
                UnityEngine.Random.InitState(seed);
                intVal = UnityEngine.Random.Range(1, 1000);
                stringVal = $"string value {UnityEngine.Random.Range(1, 1000)}";
                sub = new ComplexSubClass { floatV = UnityEngine.Random.Range(.1f, 10000f), stringVal = $"sub string value {UnityEngine.Random.Range(10000, 100000)}" };
            }

            public bool Equals(ComplexObject other) => intVal.Equals(other.intVal) && stringVal.Equals(other.stringVal) && sub.Equals(other.sub);

            public class Serializer : BinaryStorageBuffer.ISerializationAdapter<ComplexObject>
            {
                public IEnumerable<BinaryStorageBuffer.ISerializationAdapter> Dependencies => null;

                struct Data
                {
                    public struct Sub
                    {
                        public uint stringId;
                        public float floatVal;
                    }
                    public int intVal;
                    public uint stringId;
                    public uint subId;
                }

                public object Deserialize(BinaryStorageBuffer.Reader reader, Type type, uint offset, out uint size)
                {
                    var data = reader.ReadValue<Data>(offset, out var _);
                    var sub = reader.ReadValue<Data.Sub>(data.subId, out var _);
                    size = 0;
                    return new ComplexObject
                    {
                        intVal = data.intVal,
                        stringVal = reader.ReadString(data.stringId, out var _),
                        sub = new ComplexSubClass
                        {
                             floatV = sub.floatVal,
                             stringVal = reader.ReadString(sub.stringId, out var _)
                        }
                    };
                }

                public uint Serialize(BinaryStorageBuffer.Writer writer, object val)
                {
                    var co = val as ComplexObject;
                    var id = writer.Reserve<Data>();
                    var data = new Data
                    {
                        intVal = co.intVal,
                        stringId = writer.WriteString(co.stringVal),
                        subId = writer.Write(new Data.Sub
                        {
                            floatVal = co.sub.floatV,
                            stringId = writer.WriteString(co.sub.stringVal)
                        })
                    };
                    return writer.Write(id, data);
                }
            }
        }

        [Test]
        public void TestStringsAsObjects([Values("ascii string", "unicode string")] string expected, [Values(0, 10, 1024)]int cacheSize)
        {
            var wr = new BinaryStorageBuffer.Writer(1024);
            var id = wr.WriteObject(expected, false);
            var id2 = wr.WriteObject(expected, true);
            var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize);
            var str = r.ReadObject<string>(id, out var _);
            Assert.AreEqual(expected, str);
            var strObj = r.ReadObject(id2, out var _);
            Assert.AreEqual(expected, strObj);
        }

        [Test]
        public void TestMixedObjectArray([Values(0, 10, 1024)]int cacheSize)
        {
            var wr = new BinaryStorageBuffer.Writer(1024, new ComplexObject.Serializer());
            var objs = new object[] { "string val", new ComplexObject(1) };
            var id = wr.WriteObjects(objs, true);
            var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize, 0, new ComplexObject.Serializer());
            var objs2 = r.ReadObjectArray(id, out var _);
            for (int i = 0; i < objs.Length; i++)
                Assert.AreEqual(objs[i], objs2[i]);
        }

        [Test]
        public void TestComplexObjectDeduplication([Values(1024, 1024 * 1024)]int chunkSize, [Values(1, 32, 256, 1024)]int count, [Values(0, 10, 1024)]int cacheSize)
        {
            var wr = new BinaryStorageBuffer.Writer(chunkSize, new ComplexObject.Serializer());
            var ids = new uint[count];
            for (int i = 0; i < count; i++)
                ids[i] = wr.WriteObject(new ComplexObject(i), false);
            var size = wr.Length;
            for (int i = 0; i < count; i++)
                wr.WriteObject(new ComplexObject(i), false);
            Assert.Less(wr.Length, size * 2);
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize, 0, new ComplexObject.Serializer());
            for (int i = 0; i < ids.Length; i++)
                Assert.AreEqual(new ComplexObject(i), re.ReadObject<ComplexObject>(ids[i], out var _));
        }

        //https://jira.unity3d.com/browse/ADDR-3459
        [Test]
        [TestCase(short.MinValue, 0)]
        [TestCase(short.MinValue, 0)]
        [TestCase(short.MaxValue, short.MaxValue)]
        [TestCase(short.MaxValue + 1, short.MaxValue)]
        [TestCase(-1, 0)]
        public void ContentCatalogData_SerializesTimeout_Correctly(int timeout, int expectedTimeout)
        {
            AssetBundleRequestOptions options = new AssetBundleRequestOptions();
            options.Timeout = timeout;

            BinaryContentCatalogData.AssetBundleRequestOptionsSerializationAdapter adapter = new BinaryContentCatalogData.AssetBundleRequestOptionsSerializationAdapter();
            BinaryStorageBuffer.Writer writer = new BinaryStorageBuffer.Writer();
            var id = adapter.Serialize(writer, options);

            var byteArray = writer.SerializeToByteArray();
            BinaryStorageBuffer.Reader reader = new BinaryStorageBuffer.Reader(byteArray);

            var result = adapter.Deserialize(reader, typeof(AssetBundleRequestOptions), id, out var _) as AssetBundleRequestOptions;

            Assert.AreEqual(expectedTimeout, result.Timeout);
        }

        //https://jira.unity3d.com/browse/ADDR-3459
        [Test]
        [TestCase(-2, 32)]
        [TestCase(-1, 32)]
        [TestCase(128, 128)]
        [TestCase(129, 128)]
        [TestCase(0, 0)]
        public void ContentCatalogData_SerializesRedirectLimit_Correctly(int redirectLimit, int expectedRedirectLimit)
        {
            AssetBundleRequestOptions options = new AssetBundleRequestOptions();
            options.RedirectLimit = redirectLimit;

            BinaryContentCatalogData.AssetBundleRequestOptionsSerializationAdapter adapter = new BinaryContentCatalogData.AssetBundleRequestOptionsSerializationAdapter();
            BinaryStorageBuffer.Writer writer = new BinaryStorageBuffer.Writer();
            var id = adapter.Serialize(writer, options);

            var byteArray = writer.SerializeToByteArray();
            BinaryStorageBuffer.Reader reader = new BinaryStorageBuffer.Reader(byteArray);

            var result = adapter.Deserialize(reader, typeof(AssetBundleRequestOptions), id, out var _) as AssetBundleRequestOptions;

            Assert.AreEqual(expectedRedirectLimit, result.RedirectLimit);
        }

#if ENABLE_CONTENT_DIRECTORIES
        // ── ContentDirectoryAssetData round-trip tests ────────────────────────────

        static ContentDirectoryAssetData RoundTrip(ContentDirectoryAssetData input)
        {
            var adapter = new ContentDirectoryAssetData.SerializationAdapter();
            var writer = new BinaryStorageBuffer.Writer();
            var id = adapter.Serialize(writer, input);
            var bytes = writer.SerializeToByteArray();
            return adapter.Deserialize(new BinaryStorageBuffer.Reader(bytes),
                typeof(ContentDirectoryAssetData), id, out _) as ContentDirectoryAssetData;
        }

        [Test]
        public void ContentDirectoryAssetData_RegularAsset_RoundTrips()
        {
            var input = new ContentDirectoryAssetData { AssetId = 7, SceneId = -1, SubAssetIds = null };
            var result = RoundTrip(input);
            Assert.AreEqual(7, result.AssetId);
            Assert.AreEqual(-1, result.SceneId);
            Assert.IsNull(result.SubAssetIds);
        }

        [Test]
        public void ContentDirectoryAssetData_FirstAssetIndexZero_RoundTrips()
        {
            var input = new ContentDirectoryAssetData { AssetId = 0, SceneId = -1, SubAssetIds = null };
            var result = RoundTrip(input);
            Assert.AreEqual(0, result.AssetId);
            Assert.AreEqual(-1, result.SceneId);
            Assert.IsNull(result.SubAssetIds);
        }

        [Test]
        public void ContentDirectoryAssetData_SceneEntry_RoundTrips()
        {
            var input = new ContentDirectoryAssetData { AssetId = -1, SceneId = 4, SubAssetIds = null };
            var result = RoundTrip(input);
            Assert.AreEqual(-1, result.AssetId);
            Assert.AreEqual(4, result.SceneId);
            Assert.IsNull(result.SubAssetIds);
        }

        [Test]
        public void ContentDirectoryAssetData_FirstSceneIndexZero_RoundTrips()
        {
            var input = new ContentDirectoryAssetData { AssetId = -1, SceneId = 0, SubAssetIds = null };
            var result = RoundTrip(input);
            Assert.AreEqual(-1, result.AssetId);
            Assert.AreEqual(0, result.SceneId);
            Assert.IsNull(result.SubAssetIds);
        }

        [Test]
        public void ContentDirectoryAssetData_AssetWithSubAssets_RoundTrips()
        {
            var input = new ContentDirectoryAssetData { AssetId = 2, SceneId = -1, SubAssetIds = new[] { 0, 1, 3 } };
            var result = RoundTrip(input);
            Assert.AreEqual(2, result.AssetId);
            Assert.AreEqual(-1, result.SceneId);
            Assert.AreEqual(new[] { 0, 1, 3 }, result.SubAssetIds);
        }

        [Test]
        public void ContentDirectoryAssetData_EmptySubAssetArray_DeserializesAsNull()
        {
            // Length==0 is written with subAssetIdsOffset=0 (same sentinel as null),
            // so it round-trips back as null rather than an empty array.
            var input = new ContentDirectoryAssetData { AssetId = 1, SceneId = -1, SubAssetIds = new int[0] };
            var result = RoundTrip(input);
            Assert.AreEqual(1, result.AssetId);
            Assert.IsNull(result.SubAssetIds, "Empty SubAssetIds array should deserialise as null");
        }

        [Test]
        public void ContentDirectoryAssetData_NullInput_RoundTrips()
        {
            // Serialize(null) uses the ??-1 guards: both ids written as -1 so a
            // missing assetData can never be mistaken for a valid index 0.
            var result = RoundTrip(null);
            Assert.AreEqual(-1, result.AssetId);
            Assert.AreEqual(-1, result.SceneId);
            Assert.IsNull(result.SubAssetIds);
        }

        [Test]
        public void ContentDirectoryAssetData_DefaultConstructed_HasSentinelIds()
        {
            var data = new ContentDirectoryAssetData();
            Assert.AreEqual(-1, data.AssetId);
            Assert.AreEqual(-1, data.SceneId);
        }

        [Test]
        public void ContentDirectoryAssetData_MultipleEntriesInOneBuffer_RoundTrip()
        {
            // Writes three entries to a shared buffer so the sub-asset offset
            // in the first entry must survive the subsequent writes without
            // aliasing or being overwritten.
            var inputs = new[]
            {
                new ContentDirectoryAssetData { AssetId = 2, SceneId = -1, SubAssetIds = new[] { 0, 1, 3 } },
                new ContentDirectoryAssetData { AssetId = -1, SceneId = 4, SubAssetIds = null },
                new ContentDirectoryAssetData { AssetId = 9, SceneId = -1, SubAssetIds = null },
            };

            var adapter = new ContentDirectoryAssetData.SerializationAdapter();
            var writer = new BinaryStorageBuffer.Writer();
            var ids = new uint[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
                ids[i] = adapter.Serialize(writer, inputs[i]);

            var bytes = writer.SerializeToByteArray();
            var reader = new BinaryStorageBuffer.Reader(bytes);

            // Asset with sub-assets
            var r0 = adapter.Deserialize(reader, typeof(ContentDirectoryAssetData), ids[0], out _) as ContentDirectoryAssetData;
            Assert.AreEqual(2, r0.AssetId);
            Assert.AreEqual(-1, r0.SceneId);
            Assert.AreEqual(new[] { 0, 1, 3 }, r0.SubAssetIds);

            // Scene entry
            var r1 = adapter.Deserialize(reader, typeof(ContentDirectoryAssetData), ids[1], out _) as ContentDirectoryAssetData;
            Assert.AreEqual(-1, r1.AssetId);
            Assert.AreEqual(4, r1.SceneId);
            Assert.IsNull(r1.SubAssetIds);

            // Plain asset without sub-assets
            var r2 = adapter.Deserialize(reader, typeof(ContentDirectoryAssetData), ids[2], out _) as ContentDirectoryAssetData;
            Assert.AreEqual(9, r2.AssetId);
            Assert.AreEqual(-1, r2.SceneId);
            Assert.IsNull(r2.SubAssetIds);
        }
#endif
        [Test]
        public void TestComplexObjectArray([Values(1024, 1024 * 1024)]int chunkSize, [Values(1, 32, 256, 1024)]int count, [Values(0, 10, 1024)]int cacheSize)
        {
            var wr = new BinaryStorageBuffer.Writer(chunkSize, new ComplexObject.Serializer());
            var objs = new ComplexObject[count];
            for (int i = 0; i < count; i++)
                objs[i] = new ComplexObject(i);
            uint objArrayWithoutType = wr.WriteObjects(objs, false);
            uint objArrayWithType = wr.WriteObjects(objs, true);
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize, 0, new ComplexObject.Serializer());
            var typedObjs = re.ReadObjectArray<ComplexObject>(objArrayWithoutType, out var _);
            for (int i = 0; i < count; i++)
                Assert.AreEqual(objs[i], typedObjs[i]);
            var untypedObjs = re.ReadObjectArray(objArrayWithType, out var _);
            for (int i = 0; i < count; i++)
                Assert.AreEqual(objs[i], untypedObjs[i]);
        }


        [Test]
        public void TestManyComplexObjectsAndStrings([Values(1024, 1024 * 1024)]int chunkSize, [Values(1024, 10 * 1024, 100 * 1024)]int count, [Values(0, 10, 1024)]int cacheSize)
        {
            var wr = new BinaryStorageBuffer.Writer(chunkSize, new ComplexObject.Serializer());
            var ids = new uint[count];
            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 0)
                    ids[i] = wr.WriteObject(new ComplexObject(i), true);
                else
                    ids[i] = wr.WriteString($"very long start of string/a middle part that is also somewhat long/almost done.../this part is unique{i}...../this part has unicode characters, see ЁЁЁЁЁЁЁ", '/');
            }


            var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize, 0, new ComplexObject.Serializer());
            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 0)
                    Assert.AreEqual(new ComplexObject(i), r.ReadObject(ids[i], out var _, false));
                else
                    Assert.AreEqual($"very long start of string/a middle part that is also somewhat long/almost done.../this part is unique{i}...../this part has unicode characters, see ЁЁЁЁЁЁЁ", r.ReadString(ids[i], out var _, '/'));
            }
        }

        [Test]
        public void ComputeStringLength([Values("", "///", "/sadf", "wdfwef/", "/sdgf/", "///sdgfw", "adqergq///", "asff/sadgf/asdfg/werg/werg/we5rg/werg/werg/werg/werg/werg/werg")]string str)
        {
            var wr = new BinaryStorageBuffer.Writer();
            var id = wr.WriteString(str, '/');
            var data = wr.SerializeToByteArray();
            var reader = new BinaryStorageBuffer.Reader(data);
            var rStr = reader.ReadString(id, out var _, '/');
            Assert.AreEqual(str, rStr);
            var len = reader.ComputeStringLength(id, '/');
            Assert.AreEqual(str.Length, len);
        }

        class TextContextObject
        {
            public List<ComplexObject> results = new List<ComplexObject>();
        }

        [Test]
        public void TestComplexObjectArrayWithProcFunc([Values(1024, 1024 * 1024)] int chunkSize, [Values(1, 32, 256, 1024)] int count, [Values(0, 10, 1024)] int cacheSize)
        {
            var wr = new BinaryStorageBuffer.Writer(chunkSize, new ComplexObject.Serializer());
            var objs = new ComplexObject[count];
            for (int i = 0; i < count; i++)
                objs[i] = new ComplexObject(i);
            uint objArrayWithoutType = wr.WriteObjects(objs, false);
            uint objArrayWithType = wr.WriteObjects(objs, true);
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize, 0, new ComplexObject.Serializer());
            var context = new TextContextObject();
            var resultCount = re.ProcessObjectArray<ComplexObject, TextContextObject>(objArrayWithoutType, out var _, context,
                (obj, context, i, c) =>
                {
                    Assert.NotNull(obj);
                    Assert.NotNull(context);
                    context.results.Add(obj);
                });
            Assert.AreEqual(count, resultCount);
            for (int i = 0; i < count; i++)
                Assert.AreEqual(objs[i], context.results[i]);
        }

        // -----------------------------------------------------------------------
        // Regression tests added with the round of correctness fixes.
        // Each test references the bug it pins down so future churn doesn't
        // accidentally regress the behaviour.
        // -----------------------------------------------------------------------

        // Stream that returns at most `chunkSize` bytes per Read call. Mimics network /
        // compressed streams that legally short-read.
        sealed class ChunkedReadStream : Stream
        {
            readonly byte[] m_Data;
            readonly int m_ChunkSize;
            int m_Pos;
            public ChunkedReadStream(byte[] data, int chunkSize) { m_Data = data; m_ChunkSize = Math.Max(1, chunkSize); }
            public override int Read(byte[] buffer, int offset, int count)
            {
                int remaining = m_Data.Length - m_Pos;
                int toCopy = Math.Min(Math.Min(count, m_ChunkSize), remaining);
                if (toCopy <= 0) return 0;
                Buffer.BlockCopy(m_Data, m_Pos, buffer, offset, toCopy);
                m_Pos += toCopy;
                return toCopy;
            }
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => m_Data.Length;
            public override long Position { get => m_Pos; set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        // Fix #1: Reader(Stream,...) used to call Read once and ignore the return value.
        // A short-reading stream produced a buffer with trailing zeros and silent corruption.
        [Test]
        public void Reader_StreamConstructor_HandlesShortReads([Values(1, 4, 7, 64)] int chunkSize)
        {
            var wr = new BinaryStorageBuffer.Writer(1024);
            var ids = new uint[64];
            for (int i = 0; i < ids.Length; i++)
                ids[i] = wr.Write(new SimpleStruct(i + 1));
            var bytes = wr.SerializeToByteArray();

            var stream = new ChunkedReadStream(bytes, chunkSize);
            var re = new BinaryStorageBuffer.Reader(stream, (uint)bytes.Length, 0, 0);
            for (int i = 0; i < ids.Length; i++)
                Assert.AreEqual(new SimpleStruct(i + 1), re.ReadValue<SimpleStruct>(ids[i], out var _));
        }

        [Test]
        public void Reader_StreamConstructor_ThrowsOnEarlyEndOfStream()
        {
            var wr = new BinaryStorageBuffer.Writer(1024);
            wr.Write(new SimpleStruct(1));
            var bytes = wr.SerializeToByteArray();
            // Pretend the stream is bigger than it actually is.
            var stream = new MemoryStream(bytes);
            Assert.Throws<EndOfStreamException>(() =>
                new BinaryStorageBuffer.Reader(stream, (uint)bytes.Length + 32, 0, 0));
        }

        // Fix #3: ReadValue<T> used to check id >= Length but read sizeof(T) bytes,
        // so a value type straddling the buffer end could read past the array.
        [Test]
        public void ReadValue_ThrowsWhenStraddlingBufferEnd()
        {
            var wr = new BinaryStorageBuffer.Writer(1024);
            wr.Write((byte)1);
            var bytes = wr.SerializeToByteArray();
            var re = new BinaryStorageBuffer.Reader(bytes, 0);
            // Reading a long that would extend past the buffer end must throw, not silently
            // read uninitialised bytes from outside the managed array.
            Assert.Throws<Exception>(() => re.ReadValue<long>((uint)(bytes.Length - 1), out var _));
        }

        // Fix #12: ReadValueArray used to integer-divide size by sizeof(T) and silently
        // drop trailing bytes when the data wasn't aligned to T's size.
        [Test]
        public void ReadValueArray_ThrowsOnMisalignedSize()
        {
            // Write a byte[] of length 7, then try to read it as an int[].
            var wr = new BinaryStorageBuffer.Writer(1024);
            var id = wr.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7 });
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), 0);
            Assert.Throws<Exception>(() => re.ReadValueArray<int>(id, out var _));
        }

        // Fix #9 (WriteInternal empty non-prefix data hash collision) is defensive — empty
        // non-prefix writes aren't reachable from the public API (every public Write<T> path
        // either has T : unmanaged with sizeof(T) > 0 or goes through the prefix path).
        // The fix is an early-return guard rather than a behaviour change observable here.

        // Fix #25 (BuiltinTypesSerializer null return on unknown type) is defensive — the
        // Deserialize fallthrough is unreachable through the public API once #6's
        // deterministic adapter lookup is in place, so there's no behavioural test to add.

        // Fix #26: Generic type definitions and dynamic assemblies have null FullName /
        // null Assembly.FullName. The serializer now refuses rather than writing poison.
        [Test]
        public void TypeSerializer_NullFullNameThrows()
        {
            var wr = new BinaryStorageBuffer.Writer(1024);
            // typeof(List<>) is a generic type definition — FullName is non-null but a
            // generic parameter type (T from List<T>) has null FullName.
            var openParam = typeof(List<>).GetGenericArguments()[0];
            Assert.IsNull(openParam.FullName, "test precondition");
            Assert.Throws<NotSupportedException>(() => wr.WriteObject(openParam, false));
        }

        // Fix #29: WriteObjects used to call Count() then iterate the source, which gave
        // an empty second pass for non-replayable enumerables (yield/Iterator blocks).
        static IEnumerable<ComplexObject> ComplexObjectIterator(int count)
        {
            for (int i = 0; i < count; i++)
                yield return new ComplexObject(i);
        }

        [Test]
        public void WriteObjects_AcceptsNonReplayableEnumerable()
        {
            var wr = new BinaryStorageBuffer.Writer(1024, new ComplexObject.Serializer());
            var id = wr.WriteObjects(ComplexObjectIterator(8), false);
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), 1024, 0, new ComplexObject.Serializer());
            var got = re.ReadObjectArray<ComplexObject>(id, out var _);
            Assert.AreEqual(8, got.Length);
            for (int i = 0; i < 8; i++)
                Assert.AreEqual(new ComplexObject(i), got[i]);
        }

        // Fix #14: Cache hits used to return size = 0 silently. This test pins the
        // size-on-hit contract for the typed value path (the existing
        // TestDynamicStringsReturnCachedValue covers the dynamic-string path).
        [Test]
        public void Cache_HitReturnsRecordedSize()
        {
            // Use minCachedObjSize = 0 so caching is unconditional.
            var wr = new BinaryStorageBuffer.Writer(1024);
            // Write ~100 bytes of data so it's well over any threshold.
            var id = wr.Write(Enumerable.Range(0, 32).ToArray());
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), 1024, 0);

            var first = re.ReadValueArray<int>(id, out var firstSize);
            var second = re.ReadValueArray<int>(id, out var secondSize);
            Assert.AreSame(first, second, "second read should hit cache");
            Assert.AreEqual(firstSize, secondSize, "cache hit must report the same size as the original read");
        }

        // Fix #15: Write<T>(uint offset, T[]) used to skip dedup-hashing the whole array
        // and silently return uint.MaxValue if the offset wasn't found in any chunk.
        [Test]
        public void WriteAtOffset_ThrowsOnUnknownOffset()
        {
            var wr = new BinaryStorageBuffer.Writer(1024);
            // 0xDEAD_BEEF is well past anything written; not a valid reservation.
            Assert.Throws<ArgumentOutOfRangeException>(() => wr.Write(0xDEADBEEFu, new[] { 1, 2, 3 }));
        }

        [Test]
        public void WriteAtOffset_ArrayDeduplicatesAgainstSubsequentWrites()
        {
            var arr = new[] { 11, 22, 33, 44, 55 };
            var wr = new BinaryStorageBuffer.Writer(1024);
            var reservedOffset = wr.Reserve<int>((uint)arr.Length);
            wr.Write(reservedOffset, arr);
            var sizeBefore = wr.Length;
            // Writing the same array again must reuse the reservation's offset, not append.
            var second = wr.Write(arr);
            Assert.AreEqual(reservedOffset, second);
            Assert.AreEqual(sizeBefore, wr.Length);
        }

        // Fix #11 (ReadObjectArray(Type t,...) cache key) is hard to expose from the public
        // API: the binary data at any given offset has exactly one valid element-type
        // interpretation, so the dual-type cache collision the fix prevents requires
        // contrived adapter setups that don't reflect realistic usage.

        // Fix #10: Array readers used to crash with IndexOutOfRangeException on a malformed
        // id (e.g. id < sizeof(uint)). They now throw a friendly Exception with bounds info.
        [Test]
        public void ReadObjectArray_RejectsOutOfBoundsId()
        {
            var wr = new BinaryStorageBuffer.Writer(1024);
            wr.Write(42);
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), 0);
            // id = 0 means "size prefix would be at offset -4" which can't be valid.
            Assert.Throws<Exception>(() => re.ReadObjectArray(0, out var _));
        }

        // Fix #7: Adapter Dependencies used to be silently dropped if the adapter's primary
        // type slot was already occupied AND forceOverride was false. The recursive dependency
        // walk implicitly uses forceOverride=false, so a *transitive* dependency on an adapter
        // whose primary type collides with a built-in would silently fail to register the
        // *next* link in the chain.
        //
        // Chain in this test:
        //   TopAdapter (handles TopPayload)
        //     └── deps: ConflictsWithBuiltinInt (handles int — built-in already registered)
        //                  └── deps: DeepAdapter (handles DeepPayload — only this adapter knows the type)
        // With the bug: ConflictsWithBuiltinInt's deps are skipped, DeepAdapter never registers,
        //               and serialising a DeepPayload fails.
        struct TopPayload { public int v; }
        struct DeepPayload { public int v; }

        class DeepAdapter : BinaryStorageBuffer.ISerializationAdapter<DeepPayload>
        {
            public IEnumerable<BinaryStorageBuffer.ISerializationAdapter> Dependencies => null;
            public object Deserialize(BinaryStorageBuffer.Reader reader, Type t, uint offset, out uint size)
                => reader.ReadValue<DeepPayload>(offset, out size);
            public uint Serialize(BinaryStorageBuffer.Writer writer, object val) => writer.Write((DeepPayload)val);
        }

        class ConflictsWithBuiltinInt : BinaryStorageBuffer.ISerializationAdapter<int>
        {
            public IEnumerable<BinaryStorageBuffer.ISerializationAdapter> Dependencies =>
                new BinaryStorageBuffer.ISerializationAdapter[] { new DeepAdapter() };
            public object Deserialize(BinaryStorageBuffer.Reader reader, Type t, uint offset, out uint size)
                => reader.ReadValue<int>(offset, out size);
            public uint Serialize(BinaryStorageBuffer.Writer writer, object val) => writer.Write((int)val);
        }

        class TopAdapter : BinaryStorageBuffer.ISerializationAdapter<TopPayload>
        {
            public IEnumerable<BinaryStorageBuffer.ISerializationAdapter> Dependencies =>
                new BinaryStorageBuffer.ISerializationAdapter[] { new ConflictsWithBuiltinInt() };
            public object Deserialize(BinaryStorageBuffer.Reader reader, Type t, uint offset, out uint size)
                => reader.ReadValue<TopPayload>(offset, out size);
            public uint Serialize(BinaryStorageBuffer.Writer writer, object val) => writer.Write((TopPayload)val);
        }

        [Test]
        public void AdapterDependencies_TransitiveChainSurvivesIntermediateConflict()
        {
            var wr = new BinaryStorageBuffer.Writer(1024, new TopAdapter());
            // DeepPayload is two levels of dependency away, behind ConflictsWithBuiltinInt
            // whose primary slot is taken by the built-in int adapter. If transitive deps
            // were dropped, DeepAdapter would never register and the next line would fail.
            var id = wr.WriteObject(new DeepPayload { v = 99 }, false);
            Assert.AreNotEqual(uint.MaxValue, id, "DeepAdapter dependency should have been registered");

            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), 1024, 0, new TopAdapter());
            var roundtripped = re.ReadObject<DeepPayload>(id, out var _);
            Assert.AreEqual(99, roundtripped.v);
        }

        // Fix #22: Dynamic-string writing was previously recursive and could stack-overflow
        // on a string with very many small unmergeable parts. The iterative rewrite handles
        // the same input without recursion.
        [Test]
        public void WriteDynamicString_HandlesManyParts()
        {
            // Build a string with thousands of parts, each large enough to avoid merging
            // (>= sizeof(DynamicString) = 8 bytes).
            var sb = new StringBuilder();
            for (int i = 0; i < 4096; i++)
            {
                if (i > 0) sb.Append('/');
                sb.Append("partABCDE"); // 9 chars, well above 8-byte minSize
            }
            var input = sb.ToString();
            var wr = new BinaryStorageBuffer.Writer(64 * 1024);
            var id = wr.WriteString(input, '/');
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), 0);
            Assert.AreEqual(input, re.ReadString(id, out var _, '/'));
        }

        // -----------------------------------------------------------------------
        // Cache-efficiency measurement tests.
        //
        // The caching fixes (#4 polymorphic key, #5 unicode-string key alignment,
        // #11 typed-array key, #14 size-on-hit) all alter what gets cached and what
        // hits on subsequent reads. These tests quantify the difference so you can
        // run them on the pre-fix and post-fix code and compare:
        //
        //   - Same-reference count: a cache hit returns the *same instance* as the
        //     original read. ReferenceEquals = cache hit. This is the most reliable
        //     measure because it works regardless of whether stat counters are on.
        //
        //   - GetCacheStats: when BINARY_STORAGE_BUFFER_STATS is defined, prints
        //     raw request / hit counts. Otherwise reports 0 (counters compiled out).
        //
        //   - Allocation per op: GC.GetAllocatedBytesForCurrentThread before/after
        //     the measured loop. Lower = more cache hits. Strings still allocate
        //     once per cold read so a fully-warm cache should approach zero.
        // -----------------------------------------------------------------------

        // Polymorphic cache hit — pins fix #4. The adapter is registered for the
        // base type and returns a derived instance. Pre-fix the cache was keyed by
        // obj.GetType() (concrete) on add and typeof(T) (requested) on get, so
        // every re-read of a polymorphic value missed.
        abstract class Animal { public abstract string Sound(); public int Id; }
        sealed class Dog : Animal { public override string Sound() => "woof"; }

        class AnimalAdapter : BinaryStorageBuffer.ISerializationAdapter<Animal>
        {
            public IEnumerable<BinaryStorageBuffer.ISerializationAdapter> Dependencies => null;
            public uint Serialize(BinaryStorageBuffer.Writer writer, object val) => writer.Write(((Animal)val).Id);
            public object Deserialize(BinaryStorageBuffer.Reader reader, Type t, uint offset, out uint size)
                => new Dog { Id = reader.ReadValue<int>(offset, out size) };
        }

        [Test]
        public void CacheEfficiency_PolymorphicReadsHitOnReread()
        {
            var wr = new BinaryStorageBuffer.Writer(1024, new AnimalAdapter());
            const int count = 32;
            var ids = new uint[count];
            for (int i = 0; i < count; i++)
                ids[i] = wr.WriteObject(new Dog { Id = i }, false);

            // minCachedObjSize=0 → unconditional caching.
            using var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), count * 2, 0, new AnimalAdapter());

            // First read warms the cache; second read should hit and return the same instance.
            var firsts = new Animal[count];
            for (int i = 0; i < count; i++)
                firsts[i] = r.ReadObject<Animal>(ids[i], out _);

            int sameRef = 0;
            for (int i = 0; i < count; i++)
            {
                var second = r.ReadObject<Animal>(ids[i], out _);
                if (ReferenceEquals(firsts[i], second))
                    sameRef++;
            }

            // Without fix #4, sameRef would be ~0 (every re-read deserialises afresh).
            Assert.AreEqual(count, sameRef, $"Polymorphic re-reads must hit cache; got {sameRef}/{count} same-ref");
        }

        // Comprehensive efficiency report across a mixed workload. Doesn't assert a
        // hard hit-rate threshold — instead logs the numbers so you can compare
        // before/after. Run on the pre-fix code: same-ref ratios for polymorphic
        // and untyped reads should be far lower than after.
        [Test]
        public void CacheEfficiency_MixedWorkloadReport()
        {
            const int items = 200;
            const int rereadsPerItem = 5;

            var wr = new BinaryStorageBuffer.Writer(1024 * 1024, new ComplexObject.Serializer(), new AnimalAdapter());

            // A mix of read types so we exercise every cache path.
            var asciiStringIds  = new uint[items];
            var unicodeStringIds = new uint[items];
            var dynStringIds   = new uint[items];
            var typedObjIds    = new uint[items];
            var untypedObjIds  = new uint[items];
            var polyObjIds     = new uint[items];
            var valueArrayIds  = new uint[items];
            var objectArrayIds = new uint[items];

            for (int i = 0; i < items; i++)
            {
                asciiStringIds[i]   = wr.WriteString($"ascii_string_value_number_{i}_with_padding");
                unicodeStringIds[i] = wr.WriteString($"unicode_string_Ё_{i}_with_more_Ё_padding");
                dynStringIds[i]     = wr.WriteString($"some/path/to/asset/{i}/file/data.bin", '/');
                typedObjIds[i]      = wr.WriteObject(new ComplexObject(i), false);
                untypedObjIds[i]    = wr.WriteObject(new ComplexObject(i + 1000), true);
                polyObjIds[i]       = wr.WriteObject(new Dog { Id = i }, false);
                valueArrayIds[i]    = wr.Write(Enumerable.Range(i, 16).ToArray());
                var objs = new ComplexObject[8];
                for (int j = 0; j < objs.Length; j++) objs[j] = new ComplexObject(i * 100 + j);
                objectArrayIds[i]   = wr.WriteObjects(objs, false);
            }

            // Cache big enough to hold every cacheable thing this workload produces. Each
            // ComplexObject reads two inner strings, each object array adds 8 elements with
            // their own sub-entries, dyn strings cache per-part, etc.; the multiplier needs
            // headroom or partial eviction will produce false negatives in the assertions.
            using var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), items * 64, 0,
                new ComplexObject.Serializer(), new AnimalAdapter());

            // Cold pass: read each id once and remember the returned reference.
            var ascii   = new string[items];
            var unicode = new string[items];
            var dyn     = new string[items];
            var typed   = new ComplexObject[items];
            var untyped = new object[items];
            var poly    = new Animal[items];
            var valArr  = new int[items][];
            var objArr  = new ComplexObject[items][];

            for (int i = 0; i < items; i++)
            {
                ascii[i]   = r.ReadString(asciiStringIds[i], out _);
                unicode[i] = r.ReadString(unicodeStringIds[i], out _);
                dyn[i]     = r.ReadString(dynStringIds[i], out _, '/');
                typed[i]   = r.ReadObject<ComplexObject>(typedObjIds[i], out _);
                untyped[i] = r.ReadObject(untypedObjIds[i], out _);
                poly[i]    = r.ReadObject<Animal>(polyObjIds[i], out _);
                valArr[i]  = r.ReadValueArray<int>(valueArrayIds[i], out _);
                objArr[i]  = r.ReadObjectArray<ComplexObject>(objectArrayIds[i], out _, true, true);
            }

            // Snapshot stats and allocations before the warm pass.
            r.GetCacheStats(out var reqBefore, out var hitsBefore);
            long allocBefore = -1;
            try { allocBefore = GC.GetAllocatedBytesForCurrentThread(); } catch { }

            // Warm pass: re-read every id N times and count same-ref returns.
            int asciiHit = 0, unicodeHit = 0, dynHit = 0, typedHit = 0, untypedHit = 0,
                polyHit = 0, valArrHit = 0, objArrHit = 0;
            int total = 0;
            for (int rep = 0; rep < rereadsPerItem; rep++)
            {
                for (int i = 0; i < items; i++)
                {
                    if (ReferenceEquals(ascii[i],   r.ReadString(asciiStringIds[i],   out _)))            asciiHit++;
                    if (ReferenceEquals(unicode[i], r.ReadString(unicodeStringIds[i], out _)))            unicodeHit++;
                    if (ReferenceEquals(dyn[i],     r.ReadString(dynStringIds[i],     out _, '/')))       dynHit++;
                    if (ReferenceEquals(typed[i],   r.ReadObject<ComplexObject>(typedObjIds[i], out _)))  typedHit++;
                    if (ReferenceEquals(untyped[i], r.ReadObject(untypedObjIds[i], out _)))               untypedHit++;
                    if (ReferenceEquals(poly[i],    r.ReadObject<Animal>(polyObjIds[i], out _)))          polyHit++;
                    if (ReferenceEquals(valArr[i],  r.ReadValueArray<int>(valueArrayIds[i], out _)))      valArrHit++;
                    if (ReferenceEquals(objArr[i],  r.ReadObjectArray<ComplexObject>(objectArrayIds[i], out _, true, true))) objArrHit++;
                    total++;
                }
            }

            r.GetCacheStats(out var reqAfter, out var hitsAfter);
            long allocAfter = -1;
            try { allocAfter = GC.GetAllocatedBytesForCurrentThread(); } catch { }

            int warmRequests = reqAfter - reqBefore;
            int warmHits = hitsAfter - hitsBefore;

            string Pct(int hit) => $"{hit,5}/{total} ({100.0 * hit / total,5:F1}%)";
            Debug.Log("Cache efficiency — same-reference ratio across re-reads:");
            Debug.Log($"  ASCII string         {Pct(asciiHit)}");
            Debug.Log($"  Unicode string       {Pct(unicodeHit)}    [pins fix #5]");
            Debug.Log($"  Dynamic string       {Pct(dynHit)}");
            Debug.Log($"  Typed ReadObject<T>  {Pct(typedHit)}");
            Debug.Log($"  Untyped ReadObject   {Pct(untypedHit)}    [pins fix #6 outer-id cache]");
            Debug.Log($"  Polymorphic <Base>   {Pct(polyHit)}    [pins fix #4]");
            Debug.Log($"  ReadValueArray<T>    {Pct(valArrHit)}");
            Debug.Log($"  ReadObjectArray<T>   {Pct(objArrHit)}    [pins fix #11 keyed by element type]");
            if (warmRequests > 0)
                Debug.Log($"GetCacheStats hit rate (warm pass): {warmHits}/{warmRequests} ({100.0 * warmHits / warmRequests:F1}%)");
            else
                Debug.Log("GetCacheStats counters disabled — define BINARY_STORAGE_BUFFER_STATS to enable.");
            if (allocBefore >= 0 && allocAfter >= 0)
            {
                var bytesPerOp = (allocAfter - allocBefore) / (double)warmRequests;
                Debug.Log($"Allocation during warm pass: {(allocAfter - allocBefore):N0} bytes total, {bytesPerOp:F1} B/op (lower = more cache hits)");
            }

            // Sanity assertions: each path should hit cache for every re-read.
            Assert.AreEqual(total, asciiHit,   "ASCII strings should always hit cache after first read");
            Assert.AreEqual(total, unicodeHit, "Unicode strings should always hit cache after first read");
            Assert.AreEqual(total, dynHit,     "Dynamic strings should always hit cache after first read");
            Assert.AreEqual(total, typedHit,   "Typed object reads should always hit cache after first read");
            Assert.AreEqual(total, untypedHit, "Untyped object reads should always hit cache after first read");
            Assert.AreEqual(total, polyHit,    "Polymorphic reads should always hit cache after first read");
            Assert.AreEqual(total, valArrHit,  "Value arrays should always hit cache after first read");
            Assert.AreEqual(total, objArrHit,  "Object arrays should always hit cache after first read");
        }

        // ReadObjectArray<T>(id) and ReadObjectArray(typeof(T), id) must NOT share a cache
        // slot — the typed path stores T[] while the untyped path stores object[]. A shared
        // key would cause:
        //   - reference T: InvalidCastException on the (T[])object[] cast in TryGetCachedValue<T[]>
        //   - value T:     silent null return from `cached as object[]` (int[] is not object[])
        [Test]
        public void ReadObjectArray_TypedAndUntypedDoNotCollide()
        {
            var wr = new BinaryStorageBuffer.Writer(1024, new ComplexObject.Serializer());
            // Build three independent object arrays: one of int (value type), one of string
            // (sealed reference), one of ComplexObject (open reference).
            var intIds = new[] { wr.WriteObject(1, false), wr.WriteObject(2, false), wr.WriteObject(3, false) };
            var intArrayId = wr.Write(intIds);

            var strIds = new[] { wr.WriteObject("a", false), wr.WriteObject("b", false), wr.WriteObject("c", false) };
            var strArrayId = wr.Write(strIds);

            var coIds = new[] { wr.WriteObject(new ComplexObject(1), false), wr.WriteObject(new ComplexObject(2), false) };
            var coArrayId = wr.Write(coIds);

            var bytes = wr.SerializeToByteArray();

            // Scenario A: untyped read first, then typed. Pre-fix this raised
            // InvalidCastException for reference element types and silently null'd value-type
            // element reads (the cached object[] couldn't be cast to T[]).
            using (var r = new BinaryStorageBuffer.Reader(bytes, 64, 0, new ComplexObject.Serializer()))
            {
                Assert.DoesNotThrow(() =>
                {
                    var u = r.ReadObjectArray(typeof(int),    intArrayId, out _, true, true); Assert.NotNull(u);
                    var typedI = r.ReadObjectArray<int>(intArrayId, out _, true, true);
                    Assert.NotNull(typedI);
                    Assert.AreEqual(3, typedI.Length);
                    Assert.AreEqual(1, typedI[0]);
                }, "untyped→typed for value-type elements must not throw or return null");

                Assert.DoesNotThrow(() =>
                {
                    var u = r.ReadObjectArray(typeof(string), strArrayId, out _, true, true); Assert.NotNull(u);
                    var typedS = r.ReadObjectArray<string>(strArrayId, out _, true, true);
                    Assert.NotNull(typedS);
                    Assert.AreEqual("a", typedS[0]);
                }, "untyped→typed for reference-type elements must not throw");

                Assert.DoesNotThrow(() =>
                {
                    var u = r.ReadObjectArray(typeof(ComplexObject), coArrayId, out _, true, true); Assert.NotNull(u);
                    var typedC = r.ReadObjectArray<ComplexObject>(coArrayId, out _, true, true);
                    Assert.NotNull(typedC);
                    Assert.AreEqual(2, typedC.Length);
                }, "untyped→typed for adapter-handled elements must not throw");
            }

            // Scenario B: typed read first, then untyped. Pre-fix this returned null from the
            // untyped path (for value types) because `int[] as object[]` is null.
            using (var r = new BinaryStorageBuffer.Reader(bytes, 64, 0, new ComplexObject.Serializer()))
            {
                var typedI = r.ReadObjectArray<int>(intArrayId, out _, true, true);
                var untypedI = r.ReadObjectArray(typeof(int), intArrayId, out _, true, true);
                Assert.NotNull(untypedI, "typed→untyped for value-type elements must return a real object[]");
                Assert.AreEqual(typedI.Length, untypedI.Length);
                Assert.AreEqual(typedI[0], (int)untypedI[0]);

                var typedS = r.ReadObjectArray<string>(strArrayId, out _, true, true);
                var untypedS = r.ReadObjectArray(typeof(string), strArrayId, out _, true, true);
                Assert.NotNull(untypedS);
                Assert.AreEqual(typedS[0], (string)untypedS[0]);
            }

            // Same instance returned across re-reads of each path independently — confirms the
            // separated cache keys are still doing their job.
            using (var r = new BinaryStorageBuffer.Reader(bytes, 64, 0, new ComplexObject.Serializer()))
            {
                var t1 = r.ReadObjectArray<int>(intArrayId, out _, true, true);
                var t2 = r.ReadObjectArray<int>(intArrayId, out _, true, true);
                Assert.AreSame(t1, t2, "typed re-read should hit cache");

                var u1 = r.ReadObjectArray(typeof(int), intArrayId, out _, true, true);
                var u2 = r.ReadObjectArray(typeof(int), intArrayId, out _, true, true);
                Assert.AreSame(u1, u2, "untyped re-read should hit cache");

                Assert.AreNotSame(t1, u1, "typed and untyped reads must produce distinct cached arrays");
            }
        }

        // Reader pins its underlying byte[] via GCHandle for the life of the Reader. Dispose
        // releases the pin; calls after Dispose must not silently corrupt memory. The buffer
        // reference is also cleared so the byte[] is eligible for GC.
        [Test]
        public void Reader_DisposeReleasesPin()
        {
            var wr = new BinaryStorageBuffer.Writer(1024);
            var id = wr.Write(new SimpleStruct(42));
            var bytes = wr.SerializeToByteArray();

            var reader = new BinaryStorageBuffer.Reader(bytes, 0);
            Assert.AreEqual(new SimpleStruct(42), reader.ReadValue<SimpleStruct>(id, out var _));

            reader.Dispose();
            // Idempotent: a second Dispose must not crash, double-free the GCHandle, etc.
            Assert.DoesNotThrow(() => reader.Dispose());
        }

        // Fix #2: ReadDynamicString's static StringCreationState is now [ThreadStatic].
        // Concurrent reads from multiple threads previously raced on that single instance,
        // producing wrong sizes or even wrong strings.
        [Test]
        public void ReadDynamicString_IsThreadSafe()
        {
            const int strCount = 32;
            const int threadCount = 4;
            const int iterations = 200;

            var wr = new BinaryStorageBuffer.Writer(64 * 1024);
            var inputs = new string[strCount];
            var ids = new uint[strCount];
            for (int i = 0; i < strCount; i++)
            {
                // Distinct contents so any cross-talk between threads would surface.
                inputs[i] = $"thread/safety/test/string/instance_{i}/with/several/segments_{i}/end";
                ids[i] = wr.WriteString(inputs[i], '/');
            }
            // minCachedObjSize = uint.MaxValue disables caching so every read goes through
            // ReadDynamicString's full code path (the static state is what we're stressing).
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), 0, uint.MaxValue);

            var threads = new Thread[threadCount];
            var failures = 0;
            for (int t = 0; t < threadCount; t++)
            {
                threads[t] = new Thread(() =>
                {
                    for (int it = 0; it < iterations; it++)
                    {
                        for (int i = 0; i < strCount; i++)
                        {
                            var got = re.ReadString(ids[i], out var _, '/');
                            if (got != inputs[i])
                                Interlocked.Increment(ref failures);
                        }
                    }
                });
            }
            foreach (var th in threads) th.Start();
            foreach (var th in threads) th.Join();
            Assert.AreEqual(0, failures, "Concurrent reads produced incorrect strings");
        }

        // Matches the layout of BinaryStorageBuffer.TypeSerializer.Data
        [StructLayout(LayoutKind.Sequential)]
        struct TypeSerializerData
        {
            public uint assemblyId;
            public uint classId;
        }

        [Test]
        public void TypeDeserialize_ResolvesType_WhenAssemblyNotFound()
        {
            // Simulate CoreCLR scenario: assembly doesn't exist but type is resolvable by name only
            var wr = new BinaryStorageBuffer.Writer();
            var assemblyId = wr.WriteString("NonExistentAssembly.ForTesting", '.');
            var classId = wr.WriteString("System.String", '.');
            var dataId = wr.Write(new TypeSerializerData { assemblyId = assemblyId, classId = classId });

            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray());
            var resolvedType = re.ReadObject<Type>(dataId, out var _);
            Assert.AreEqual(typeof(string), resolvedType);
        }

        [Test]
        public void TypeDeserialize_ReturnsNull_WhenTypeCannotBeResolved()
        {
            // Both assembly and type are non-existent — all fallbacks return null
            var wr = new BinaryStorageBuffer.Writer();
            var assemblyId = wr.WriteString("NonExistentAssembly.ForTesting", '.');
            var classId = wr.WriteString("NonExistent.FakeType.ForTesting", '.');
            var dataId = wr.Write(new TypeSerializerData { assemblyId = assemblyId, classId = classId });

            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray());
            var resolvedType = re.ReadObject<Type>(dataId, out var _);
            Assert.IsNull(resolvedType);
        }

        [Test]
        [TestCase("System.Int32", typeof(int))]
        [TestCase("System.Boolean", typeof(bool))]
        [TestCase("System.Int64", typeof(long))]
        public void TypeDeserialize_ResolvesCommonTypes_WhenAssemblyNotFound(string typeName, Type expected)
        {
            var wr = new BinaryStorageBuffer.Writer();
            var assemblyId = wr.WriteString("mscorlib.Fake", '.');
            var classId = wr.WriteString(typeName, '.');
            var dataId = wr.Write(new TypeSerializerData { assemblyId = assemblyId, classId = classId });

            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray());
            var resolvedType = re.ReadObject<Type>(dataId, out var _);
            Assert.AreEqual(expected, resolvedType);
        }

        [Test]
        public void TypeRoundTrip_Corelib_UsesNullAssemblySentinel()
        {
            var wr = new BinaryStorageBuffer.Writer();
            var typeId = wr.WriteObject(typeof(string), false);
            var bytes = wr.SerializeToByteArray();

            var re = new BinaryStorageBuffer.Reader(bytes);
            // Inspect the raw on-disk Data to confirm the corelib assembly is encoded as the null sentinel.
            var data = re.ReadValue<TypeSerializerData>(typeId, out _);
            Assert.AreEqual(uint.MaxValue, data.assemblyId, "corelib assembly should be encoded as uint.MaxValue (null sentinel)");

            var resolved = re.ReadObject<Type>(typeId, out _);
            Assert.AreEqual(typeof(string), resolved);
        }

        [Test]
        public void TypeRoundTrip_NonCore_StripsVersionInfo()
        {
            var wr = new BinaryStorageBuffer.Writer();
            var typeId = wr.WriteObject(typeof(UnityEngine.Vector3), false);
            var bytes = wr.SerializeToByteArray();

            var re = new BinaryStorageBuffer.Reader(bytes);
            var data = re.ReadValue<TypeSerializerData>(typeId, out _);
            var assemblyName = re.ReadString(data.assemblyId, out _, '.');
            Assert.AreEqual("UnityEngine.CoreModule", assemblyName, "non-corelib assembly should be the simple name only");
            Assert.IsFalse(assemblyName.Contains("Version="), "version info must be stripped");

            var resolved = re.ReadObject<Type>(typeId, out _);
            Assert.AreEqual(typeof(UnityEngine.Vector3), resolved);
        }

        [Test]
        public void TypeRoundTrip_GenericOverUserType()
        {
            var wr = new BinaryStorageBuffer.Writer();
            var t = typeof(List<SimpleStruct>);
            var typeId = wr.WriteObject(t, false);
            var bytes = wr.SerializeToByteArray();

            var re = new BinaryStorageBuffer.Reader(bytes);
            var data = re.ReadValue<TypeSerializerData>(typeId, out _);
            var className = re.ReadString(data.classId, out _, '.');
            Assert.IsFalse(className.Contains("Version="), "generic argument identity must not embed Version=");
            Assert.IsFalse(className.Contains("PublicKeyToken="), "generic argument identity must not embed PublicKeyToken=");

            var resolved = re.ReadObject<Type>(typeId, out _);
            Assert.AreEqual(t, resolved);
        }

        [Test]
        public void TypeRoundTrip_ArrayType()
        {
            var wr = new BinaryStorageBuffer.Writer();
            var typeId = wr.WriteObject(typeof(UnityEngine.Vector3[]), false);
            var resolved = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray()).ReadObject<Type>(typeId, out _);
            Assert.AreEqual(typeof(UnityEngine.Vector3[]), resolved);
        }

        class OuterTestType { public class InnerTestType { } }

        [Test]
        public void TypeRoundTrip_NestedType()
        {
            var wr = new BinaryStorageBuffer.Writer();
            var typeId = wr.WriteObject(typeof(OuterTestType.InnerTestType), false);
            var resolved = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray()).ReadObject<Type>(typeId, out _);
            Assert.AreEqual(typeof(OuterTestType.InnerTestType), resolved);
        }

        [Test]
        public void TypeNameResolver_NullAssembly_ResolvesCorelibType()
        {
            Assert.AreEqual(typeof(string), TypeNameResolver.Resolve(null, "System.String"));
            Assert.AreEqual(typeof(int), TypeNameResolver.Resolve("", "System.Int32"));
        }

        [Test]
        public void TypeNameResolver_UnknownAssembly_ReturnsNullNoThrow()
        {
            Assert.IsNull(TypeNameResolver.Resolve("Definitely.Not.A.Real.Assembly.ZZZ", "Definitely.Not.A.Real.Type.ZZZ"));
        }

        [Test]
        public void TypeNameResolver_GetSimpleAssemblyName_NullForCorelib()
        {
            Assert.IsNull(TypeNameResolver.GetSimpleAssemblyName(typeof(string)));
            Assert.AreEqual("UnityEngine.CoreModule", TypeNameResolver.GetSimpleAssemblyName(typeof(UnityEngine.Vector3)));
        }

        [Test]
        public void TypeNameResolver_Initialize_IsIdempotentAndResolveStillWorks()
        {
            // Repeated calls must not throw, and resolution must keep working.
            TypeNameResolver.Initialize();
            TypeNameResolver.Initialize();
            TypeNameResolver.Initialize();

            Assert.AreEqual(typeof(string), TypeNameResolver.Resolve(null, "System.String"));
            Assert.AreEqual(typeof(UnityEngine.Vector3), TypeNameResolver.Resolve("UnityEngine.CoreModule", "UnityEngine.Vector3"));
        }

        [Test]
        public void NormalizeTypeName_CorelibGenericArguments_OmitRuntimeSpecificAssembly()
        {
            var corelibName = typeof(object).Assembly.GetName().Name;
            foreach (var t in new[] { typeof(List<string>), typeof(Dictionary<string, int>), typeof(List<List<string>>) })
            {
                var name = TypeNameResolver.NormalizeTypeName(t);
                Assert.IsFalse(name.Contains(corelibName), $"normalized name '{name}' embeds the writer's corelib identity '{corelibName}'");
                Assert.AreEqual(t, TypeNameResolver.Resolve(TypeNameResolver.GetSimpleAssemblyName(t), name));
            }
        }

        [Test]
        [TestCase("mscorlib")]
        [TestCase("System.Private.CoreLib")]
        public void TypeNameResolver_NullAssembly_ResolvesGenericWithForeignCorelibArgs(string corelib)
        {
            // The writer no longer emits these names, but legacy/foreign-runtime catalogs contain them.
            var typeName = "System.Collections.Generic.List`1[[System.String, " + corelib + "]]";
            Assert.AreEqual(typeof(List<string>), TypeNameResolver.Resolve(null, typeName));
        }

        [Test]
        public void TypeRoundTrip_GenericOverCorelibType()
        {
            var wr = new BinaryStorageBuffer.Writer();
            var t = typeof(Dictionary<string, int>);
            var typeId = wr.WriteObject(t, false);
            var bytes = wr.SerializeToByteArray();

            var re = new BinaryStorageBuffer.Reader(bytes);
            var data = re.ReadValue<TypeSerializerData>(typeId, out _);
            Assert.AreEqual(uint.MaxValue, data.assemblyId, "corelib outer assembly should be encoded as the null sentinel");
            var className = re.ReadString(data.classId, out _, '.');
            var corelibName = typeof(object).Assembly.GetName().Name;
            Assert.IsFalse(className.Contains(corelibName), $"serialized class name '{className}' embeds the writer's corelib identity '{corelibName}'");

            var resolved = re.ReadObject<Type>(typeId, out _);
            Assert.AreEqual(t, resolved);
        }
    }
}
