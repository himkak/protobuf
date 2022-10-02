﻿using Google.Protobuf.Reflection.Dynamic;
using Google.Protobuf.TestProtos;
using Google.Protobuf.WellKnownTypes;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Google.Protobuf.Reflection
{
    public class DynamicMessageTest
    {

        /*[Test]
        public void TestDynamicMessageParsing()
        {
            MessageDescriptor msgDesc = TestAllTypes.Descriptor;
            //string msg = "{ \"TestAllTypes\":{\"single_int32\":1}}";
            string msg = "{\"single_int32\":1}";
            DynamicMessage.Builder dmBuilder = DynamicMessage.NewBuilder(msgDesc);
            dmBuilder.SetField(msgDesc.FindFieldByName("single_int32"), 1);
            ByteString bs = ByteString.AttachBytes(dmBuilder.Build().ToByteArray());
            DynamicMessage dm = DynamicMessage.ParseFrom(msgDesc, bs);
            Assert.NotNull(dm);
        }*/

        [Test]
        public void TestDynamicMessageParsing1()
        {
            string val = "str1";
            TestAllTypes msg = new()
            {
                SingleString = val
            };

            ByteString byteStr = msg.ToByteString();
            MessageDescriptor desc = TestAllTypes.Descriptor;
            FieldDescriptor fd = desc.FindFieldByName("single_string");
            DynamicMessage res = DynamicMessage.ParseFrom(desc, byteStr);
            Assert.NotNull(res);
            Assert.AreEqual(val, res.GetField(fd));
        }

        [Test]
        public void TestDynamicMessageParsingAllTypes()
        {
            TestAllTypes message = new TestAllTypes
            {
                SingleBool = true,
                SingleBytes = ByteString.CopyFrom(1, 2, 3, 4),
                SingleDouble = 23.5,
                SingleFixed32 = 23,
                SingleFixed64 = 1234567890123,
                SingleFloat = 12.25f,
                SingleForeignEnum = ForeignEnum.ForeignBar,
                SingleForeignMessage = new ForeignMessage { C = 10 },
                SingleImportEnum = ImportEnum.ImportBaz,
                SingleImportMessage = new ImportMessage { D = 20 },
                SingleInt32 = 100,
                SingleInt64 = 3210987654321,
                SingleNestedEnum = TestProtos.TestAllTypes.Types.NestedEnum.Foo,
                SingleNestedMessage = new TestAllTypes.Types.NestedMessage { Bb = 35 },
                SinglePublicImportMessage = new PublicImportMessage { E = 54 },
                SingleSfixed32 = -123,
                SingleSfixed64 = -12345678901234,
                SingleSint32 = -456,
                SingleSint64 = -12345678901235,
                SingleString = "test",
                SingleUint32 = UInt32.MaxValue,
                SingleUint64 = UInt64.MaxValue,
                RepeatedBool = { true, false },
                RepeatedBytes = { ByteString.CopyFrom(1, 2, 3, 4), ByteString.CopyFrom(5, 6), ByteString.CopyFrom(new byte[1000]) },
                RepeatedDouble = { -12.25, 23.5 },
                RepeatedFixed32 = { UInt32.MaxValue, 23 },
                RepeatedFixed64 = { UInt64.MaxValue, 1234567890123 },
                RepeatedFloat = { 100f, 12.25f },
                //RepeatedForeignEnum = { ForeignEnum.ForeignFoo, ForeignEnum.ForeignBar },
                RepeatedForeignMessage = { new ForeignMessage(), new ForeignMessage { C = 10 } },
                //RepeatedImportEnum = { ImportEnum.ImportBaz, ImportEnum.Unspecified },
                RepeatedImportMessage = { new ImportMessage { D = 20 }, new ImportMessage { D = 25 } },
                RepeatedInt32 = { 100, 200 },
                RepeatedInt64 = { 3210987654321, Int64.MaxValue },
                //RepeatedNestedEnum = { TestProtos.TestAllTypes.Types.NestedEnum.Foo, TestProtos.TestAllTypes.Types.NestedEnum.Neg },
                RepeatedNestedMessage = { new TestAllTypes.Types.NestedMessage { Bb = 35 }, new TestAllTypes.Types.NestedMessage { Bb = 10 } },
                RepeatedPublicImportMessage = { new PublicImportMessage { E = 54 }, new PublicImportMessage { E = -1 } },
                RepeatedSfixed32 = { -123, 123 },
                RepeatedSfixed64 = { -12345678901234, 12345678901234 },
                RepeatedSint32 = { -456, 100 },
                RepeatedSint64 = { -12345678901235, 123 },
                RepeatedString = { "foo", "bar" },
                RepeatedUint32 = { UInt32.MaxValue, UInt32.MinValue },
                RepeatedUint64 = { UInt64.MaxValue, UInt32.MinValue },
                OneofString = "Oneof string"
            };
            MessageDescriptor desc = TestAllTypes.Descriptor;
            ByteString byteStr = message.ToByteString();

            DynamicMessage res = DynamicMessage.ParseFrom(desc, byteStr);
            Assertions(desc, res);

        }

        private void Assertions(MessageDescriptor desc, DynamicMessage res)
        {
            Assert.NotNull(res);

            Assert.AreEqual("test", GetField(desc, res, "single_string"));

            Assert.AreEqual(100, GetField(desc, res, "repeated_int32[0]"));

            Assert.AreEqual(4294967295, GetField(desc, res, "repeated_fixed32[0]"));

            Assert.AreEqual(10, GetField(desc, res, "single_foreign_message.c"));

            Assert.AreEqual(20, GetField(desc, res, "single_import_message.d"));

            Assert.AreEqual(54, GetField(desc, res, "single_public_import_message.e"));

            Assert.AreEqual((int) ForeignEnum.ForeignBar, GetField(desc, res, "single_foreign_enum"));

            /*FieldDescriptor repeatedForeignEnum = desc.FindFieldByName("repeated_foreign_enum");
            List<object> repeatedForeignEnumList = (List<object>) res.GetField(repeatedForeignEnum);
            Assert.AreEqual((int) ForeignEnum.ForeignFoo, repeatedForeignEnumList[0]);
            Assert.AreEqual((int) ForeignEnum.ForeignBar, repeatedForeignEnumList[1]);*/


            Assert.AreEqual(10, GetField(desc, res, "repeated_foreign_message[1].c"));

            Assert.AreEqual(20, GetField(desc, res, "repeated_import_message[0].d"));
            Assert.AreEqual(25, GetField(desc, res, "repeated_import_message[1].d"));

            Assert.AreEqual(54, GetField(desc, res, "repeated_public_import_message[0].e"));
            Assert.AreEqual(-1, GetField(desc, res, "repeated_public_import_message[1].e"));

            Assert.AreEqual(35, GetField(desc, res, "repeated_nested_message[0].bb"));
            Assert.AreEqual(10, GetField(desc, res, "repeated_nested_message[1].bb"));
        }

        private object GetField(MessageDescriptor desc, DynamicMessage dm, String fieldFullName)
        {
            if (!fieldFullName.Contains(".") && !fieldFullName.Contains("["))
            {
                return dm.GetField(desc.FindFieldByName(fieldFullName));
            }
            else if (fieldFullName.Contains("[") && !fieldFullName.Contains("."))
            {
                string fieldName = fieldFullName.Substring(0, fieldFullName.IndexOf("["));
                int index = int.Parse(fieldFullName.Substring(fieldFullName.IndexOf("[") + 1, 1));
                List<object> list = (List<object>) GetField(desc, dm, fieldName);
                Assert.NotNull(list);
                return list[index];
            }
            else
            {
                string[] fields = fieldFullName.Split('.');
                DynamicMessage tempDm = dm;
                MessageDescriptor tempDesc = desc;
                for (int i = 0; i < fields.Length; i++)
                {
                    string field = fields[i];

                    if (i == fields.Length - 1)
                        return GetField(tempDesc, tempDm, field);
                    else
                    {
                        tempDm = (DynamicMessage) GetField(tempDesc, tempDm, field);
                        string tempField = field;
                        if (field.Contains("["))
                        {
                            tempField = field.Substring(0, field.IndexOf("["));
                        }
                        tempDesc = tempDesc.FindFieldByName(tempField).MessageType;
                    }

                }
            }
            return null;
        }


        [Test]
        public void TestDynamicMessageWriteTo()
        {

            TestAllTypes message = new TestAllTypes
            {
                SingleBool = true,
                SingleBytes = ByteString.CopyFrom(1, 2, 3, 4),
                SingleDouble = 23.5,
                SingleFixed32 = 23,
                SingleFixed64 = 1234567890123,
                SingleFloat = 12.25f,
                SingleForeignEnum = ForeignEnum.ForeignBar,
                SingleForeignMessage = new ForeignMessage { C = 10 },
                SingleImportEnum = ImportEnum.ImportBaz,
                SingleImportMessage = new ImportMessage { D = 20 },
                SingleInt32 = 100,
                SingleInt64 = 3210987654321,
                SingleNestedEnum = TestProtos.TestAllTypes.Types.NestedEnum.Foo,
                SingleNestedMessage = new TestAllTypes.Types.NestedMessage { Bb = 35 },
                SinglePublicImportMessage = new PublicImportMessage { E = 54 },
                SingleSfixed32 = -123,
                SingleSfixed64 = -12345678901234,
                SingleSint32 = -456,
                SingleSint64 = -12345678901235,
                SingleString = "test",
                SingleUint32 = UInt32.MaxValue,
                SingleUint64 = UInt64.MaxValue,
                RepeatedBool = { true, false },
                RepeatedBytes = { ByteString.CopyFrom(1, 2, 3, 4), ByteString.CopyFrom(5, 6), ByteString.CopyFrom(new byte[1000]) },
                RepeatedDouble = { -12.25, 23.5 },
                RepeatedFixed32 = { UInt32.MaxValue, 23 },
                RepeatedFixed64 = { UInt64.MaxValue, 1234567890123 },
                RepeatedFloat = { 100f, 12.25f },
                //RepeatedForeignEnum = { ForeignEnum.ForeignFoo, ForeignEnum.ForeignBar },
                RepeatedForeignMessage = { new ForeignMessage(), new ForeignMessage { C = 10 } },
                //RepeatedImportEnum = { ImportEnum.ImportBaz, ImportEnum.Unspecified },
                RepeatedImportMessage = { new ImportMessage { D = 20 }, new ImportMessage { D = 25 } },
                RepeatedInt32 = { 100, 200 },
                RepeatedInt64 = { 3210987654321, Int64.MaxValue },
                //RepeatedNestedEnum = { TestProtos.TestAllTypes.Types.NestedEnum.Foo, TestProtos.TestAllTypes.Types.NestedEnum.Neg },
                RepeatedNestedMessage = { new TestAllTypes.Types.NestedMessage { Bb = 35 }, new TestAllTypes.Types.NestedMessage { Bb = 10 } },
                RepeatedPublicImportMessage = { new PublicImportMessage { E = 54 }, new PublicImportMessage { E = -1 } },
                RepeatedSfixed32 = { -123, 123 },
                RepeatedSfixed64 = { -12345678901234, 12345678901234 },
                RepeatedSint32 = { -456, 100 },
                RepeatedSint64 = { -12345678901235, 123 },
                RepeatedString = { "foo", "bar" },
                RepeatedUint32 = { UInt32.MaxValue, UInt32.MinValue },
                RepeatedUint64 = { UInt64.MaxValue, UInt32.MinValue },
                OneofString = "Oneof string"
            };
            MessageDescriptor desc = TestAllTypes.Descriptor;
            ByteString byteStr = message.ToByteString();

            DynamicMessage dm = DynamicMessage.ParseFrom(desc, byteStr);
            DynamicMessage objectUnderTest = DynamicMessage.ParseFrom(desc, Any.Pack(dm).Value);

            //Assert.AreEqual(dm.CalculateSize(), objectUnderTest.CalculateSize());

            //Assert.AreEqual("test", GetField(desc, objectUnderTest, "single_string"));

            Assertions(desc, objectUnderTest);

        }

    }
}
