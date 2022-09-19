using Google.Protobuf.Reflection.Dynamic;
using Google.Protobuf.TestProtos;
using NUnit.Framework;
using System.Collections.Generic;

namespace Google.Protobuf.Reflection
{
    public class DynamicMessageTest
    {

        [Test]
        public void TestDynamicMessageParsing()
        {
            MessageDescriptor msgDesc = TestAllTypes.Descriptor;
            //string msg = "{ \"TestAllTypes\":{\"single_int32\":1}}";
            string msg = "{\"single_int32\":1}";
            DynamicMessage.Builder dmBuilder = DynamicMessage.CreateBuilder(msgDesc);
            dmBuilder.SetField(msgDesc.FindFieldByName("single_int32"), 1);
            ByteString bs = ByteString.AttachBytes(dmBuilder.Build().ToByteArray());

            /*byte[] data = Encoding.UTF8.GetBytes(msg);
            string base64 = Convert.ToBase64String(data);
            ByteString bs = ByteString.FromBase64(base64);*/
            DynamicMessage dm = DynamicMessage.ParseFrom(msgDesc, bs);
            Assert.NotNull(dm);

        }

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
            TestAllTypes message = SampleMessages.CreateFullTestAllTypes();
            MessageDescriptor desc = TestAllTypes.Descriptor;
            ByteString byteStr = message.ToByteString();

            DynamicMessage res = DynamicMessage.ParseFrom(desc, byteStr);


            Assert.NotNull(res);
            Assert.AreEqual("test", res.GetField(desc.FindFieldByName("single_string")));
            List<object> list = (List<object>) res.GetField(desc.FindFieldByName("repeated_int32"));
            Assert.NotNull(list);
            Assert.AreEqual(100, list[0]);

            List<object> list1 = (List<object>) res.GetField(desc.FindFieldByName("repeated_fixed32"));
            Assert.NotNull(list1);
            Assert.AreEqual(4294967295, list1[0]);

            FieldDescriptor foreignMsgDesc = desc.FindFieldByName("single_foreign_message");
            DynamicMessage foreignMessage = (DynamicMessage) res.GetField(foreignMsgDesc);
            Assert.AreEqual(10, foreignMessage.GetField(foreignMsgDesc.MessageType.FindFieldByName("c")));

            FieldDescriptor singleImportMsgDesc = desc.FindFieldByName("single_import_message");
            DynamicMessage singleImportMessage = (DynamicMessage) res.GetField(singleImportMsgDesc);
            Assert.AreEqual(20, singleImportMessage.GetField(singleImportMsgDesc.MessageType.FindFieldByName("d")));

            FieldDescriptor singlePublicImportMsgDesc = desc.FindFieldByName("single_public_import_message");
            DynamicMessage singlePublicImportMessage = (DynamicMessage) res.GetField(singlePublicImportMsgDesc);
            Assert.AreEqual(54, singlePublicImportMessage.GetField(singlePublicImportMsgDesc.MessageType.FindFieldByName("e")));

            FieldDescriptor singleForeignEnum = desc.FindFieldByName("single_foreign_enum");
            Assert.AreEqual((int) ForeignEnum.ForeignBar, res.GetField(singleForeignEnum));

            /*FieldDescriptor repeatedForeignEnum = desc.FindFieldByName("repeated_foreign_enum");
            List<object> repeatedForeignEnumList = (List<object>) res.GetField(repeatedForeignEnum);
            Assert.AreEqual((int) ForeignEnum.ForeignFoo, repeatedForeignEnumList[0]);
            Assert.AreEqual((int) ForeignEnum.ForeignBar, repeatedForeignEnumList[1]);*/


            FieldDescriptor repeatedForeignMsgDesc = desc.FindFieldByName("repeated_foreign_message");
            List<object> listRepeatedForeignMessage = (List<object>) res.GetField(repeatedForeignMsgDesc);
            DynamicMessage repeatedForeignMessage = (DynamicMessage) listRepeatedForeignMessage[1];
            repeatedForeignMsgDesc = repeatedForeignMsgDesc.MessageType.FindFieldByName("c");
            Assert.AreEqual(10, repeatedForeignMessage.GetField(repeatedForeignMsgDesc));

            FieldDescriptor repeatedImportMsgDesc = desc.FindFieldByName("repeated_import_message");
            List<object> listRepeatedImportMessage = (List<object>) res.GetField(repeatedImportMsgDesc);
            repeatedImportMsgDesc = repeatedImportMsgDesc.MessageType.FindFieldByName("d");
            Assert.AreEqual(20, ((DynamicMessage) listRepeatedImportMessage[0]).GetField(repeatedImportMsgDesc));
            Assert.AreEqual(25, ((DynamicMessage) listRepeatedImportMessage[1]).GetField(repeatedImportMsgDesc));

            FieldDescriptor repeatedPublicImportMsgDesc = desc.FindFieldByName("repeated_public_import_message");
            List<object> listRepeatedPublicImportMessage = (List<object>) res.GetField(repeatedPublicImportMsgDesc);
            repeatedPublicImportMsgDesc = repeatedPublicImportMsgDesc.MessageType.FindFieldByName("e");
            Assert.AreEqual(54, ((DynamicMessage) listRepeatedPublicImportMessage[0]).GetField(repeatedPublicImportMsgDesc));
            Assert.AreEqual(-1, ((DynamicMessage) listRepeatedPublicImportMessage[1]).GetField(repeatedPublicImportMsgDesc));

            FieldDescriptor repeatedNestedMsgDesc = desc.FindFieldByName("repeated_nested_message");
            List<object> listRepeatedNestedMessage = (List<object>) res.GetField(repeatedNestedMsgDesc);
            repeatedNestedMsgDesc = repeatedNestedMsgDesc.MessageType.FindFieldByName("bb");
            Assert.AreEqual(35, ((DynamicMessage) listRepeatedNestedMessage[0]).GetField(repeatedNestedMsgDesc));
            Assert.AreEqual(10, ((DynamicMessage) listRepeatedNestedMessage[1]).GetField(repeatedNestedMsgDesc));

        }


    }
}
