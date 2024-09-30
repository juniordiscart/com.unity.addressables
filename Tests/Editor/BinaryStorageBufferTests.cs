#if UNITY_2020_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
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
            Assert.AreEqual(5, re.ReadValue<int>(intID));
            Assert.AreEqual(true, re.ReadValue<bool>(boolId));
            Assert.AreEqual(new SimpleStruct(1), re.ReadValue<SimpleStruct>(structId1));
            Assert.AreEqual(new SimpleStruct(2), re.ReadValue<SimpleStruct>(structId2));
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
            Assert.AreEqual(5, re.ReadValue<int>(intID));
            Assert.AreEqual(true, re.ReadValue<bool>(boolId));
            Assert.AreEqual(new SimpleStruct(1), re.ReadValue<SimpleStruct>(structId1));
            Assert.AreEqual(new SimpleStruct(2), re.ReadValue<SimpleStruct>(structId2));
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
            Assert.AreEqual(5, re.ReadValue<int>(intID));
            Assert.AreEqual(true, re.ReadValue<bool>(boolId));
            Assert.AreEqual(new SimpleStruct(1), re.ReadValue<SimpleStruct>(structId1));
            Assert.AreEqual(new SimpleStruct(2), re.ReadValue<SimpleStruct>(structId2));
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
            var a1 = re.ReadValueArray<SimpleStruct>(arrayId);
            var a2 = re.ReadValueArray<SimpleStruct>(arrayId2);
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
            var rStr = re.ReadObject(str) as string;
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
            var strRes = re.ReadString(str, sep);
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
            Assert.AreEqual(txt, re.ReadString(str, sep));
            Assert.AreEqual(txt, re.ReadString(str2, sep));
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
            var strRes = re.ReadString(str, sep);
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
            var str2 = r.ReadString(id, '/');
            Assert.AreEqual(str, str2);
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
                r.ReadString(ids[UnityEngine.Random.Range(0, ids.Length)], '/');
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

                public object Deserialize(BinaryStorageBuffer.Reader reader, Type type, uint offset)
                {
                    var data = reader.ReadValue<Data>(offset);
                    var sub = reader.ReadValue<Data.Sub>(data.subId);
                    return new ComplexObject
                    {
                        intVal = data.intVal,
                        stringVal = reader.ReadString(data.stringId),
                        sub = new ComplexSubClass
                        {
                             floatV = sub.floatVal,
                             stringVal = reader.ReadString(sub.stringId)
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
            var str = r.ReadObject<string>(id);
            Assert.AreEqual(expected, str);
            var strObj = r.ReadObject(id2);
            Assert.AreEqual(expected, strObj);
        }

        [Test]
        public void TestMixedObjectArray([Values(0, 10, 1024)]int cacheSize)
        {
            var wr = new BinaryStorageBuffer.Writer(1024, new ComplexObject.Serializer());
            var objs = new object[] { "string val", new ComplexObject(1) };
            var id = wr.WriteObjects(objs, true);
            var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize, new ComplexObject.Serializer());
            var objs2 = r.ReadObjectArray(id);
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
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize, new ComplexObject.Serializer());
            for (int i = 0; i < ids.Length; i++)
                Assert.AreEqual(new ComplexObject(i), re.ReadObject<ComplexObject>(ids[i]));
        }

        [Test]
        public void TestComplexObjectArray([Values(1024, 1024 * 1024)]int chunkSize, [Values(1, 32, 256, 1024)]int count, [Values(0, 10, 1024)]int cacheSize)
        {
            var wr = new BinaryStorageBuffer.Writer(chunkSize, new ComplexObject.Serializer());
            var objs = new ComplexObject[count];
            for (int i = 0; i < count; i++)
                objs[i] = new ComplexObject(i);
            uint objArrayWithoutType = wr.WriteObjects(objs, false);
            uint objArrayWithType = wr.WriteObjects(objs, true);
            var re = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize, new ComplexObject.Serializer());
            var typedObjs = re.ReadObjectArray<ComplexObject>(objArrayWithoutType);
            for (int i = 0; i < count; i++)
                Assert.AreEqual(objs[i], typedObjs[i]);
            var untypedObjs = re.ReadObjectArray(objArrayWithType);
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


            var r = new BinaryStorageBuffer.Reader(wr.SerializeToByteArray(), cacheSize, new ComplexObject.Serializer());
            for (int i = 0; i < count; i++)
            {
                if (i % 2 == 0)
                    Assert.AreEqual(new ComplexObject(i), r.ReadObject(ids[i], false));
                else
                    Assert.AreEqual($"very long start of string/a middle part that is also somewhat long/almost done.../this part is unique{i}...../this part has unicode characters, see ЁЁЁЁЁЁЁ", r.ReadString(ids[i], '/'));
            }
        }
    }
}
#endif
