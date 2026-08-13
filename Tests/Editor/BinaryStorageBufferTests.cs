using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
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
            Assert.AreEqual(0, str2Size);
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
             "a/b/wergwegbwethgrwtherth/Ёc/e/ffdsfsЁrgwetghwthwrh/e/s/wergwethgwrthrewthweЁr"
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
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 100000; i++)
                r.ReadString(ids[UnityEngine.Random.Range(0, ids.Length)], out var _, '/');
            sw.Stop();
            Debug.Log($"ReadString time: {sw.ElapsedMilliseconds}ms.");
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

#if !ENABLE_JSON_CATALOG
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

            ContentCatalogData.AssetBundleRequestOptionsSerializationAdapter adapter = new ContentCatalogData.AssetBundleRequestOptionsSerializationAdapter();
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

            ContentCatalogData.AssetBundleRequestOptionsSerializationAdapter adapter = new ContentCatalogData.AssetBundleRequestOptionsSerializationAdapter();
            BinaryStorageBuffer.Writer writer = new BinaryStorageBuffer.Writer();
            var id = adapter.Serialize(writer, options);

            var byteArray = writer.SerializeToByteArray();
            BinaryStorageBuffer.Reader reader = new BinaryStorageBuffer.Reader(byteArray);

            var result = adapter.Deserialize(reader, typeof(AssetBundleRequestOptions), id, out var _) as AssetBundleRequestOptions;

            Assert.AreEqual(expectedRedirectLimit, result.RedirectLimit);
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

    }
}
