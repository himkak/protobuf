//using Google.ProtocolBuffers.Descriptors;

using Google.Protobuf.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Google.Protobuf.Reflection.Dynamic
{

    /// <summary>
    /// An implementation of IMessage that can represent arbitrary types, given a MessageaDescriptor.
    /// </summary>
    public sealed partial class DynamicMessage : IMessage
    {
        private readonly MessageDescriptor type;
        private readonly FieldSet fields;
        private readonly UnknownFieldSet unknownFields;
        private int memoizedSize = -1;

        public MessageDescriptor Descriptor => type;


        /// <summary>
        /// Constructs a builder for a message of the same type as <paramref name="prototype"/>,
        /// and initializes it with the same contents.
        /// </summary>
        /// <param name="prototype"></param>
        /// <returns></returns>
        public static Builder CreateBuilder(MessageDescriptor prototype)
        {
            return new Builder(prototype);
        }

        public void MergeFrom(CodedInputStream input)
        {
            MergeFrom(input);
        }

        public void WriteTo(CodedOutputStream output)
        {
            fields.WriteTo(output);
        }

        public int CalculateSize()
        {
            int size = memoizedSize;
            if (size != -1)
            {
                return size;
            }
            size = fields.getSerializedSize();
            memoizedSize = size;
            return size;
        }

        public static DynamicMessage ParseFrom(MessageDescriptor type, ByteString data)
        {
            Builder builder = NewBuilder(type);
            //Console.WriteLine(data.ToStringUtf8());
            builder.MergeFrom(data);
            return builder.BuildParsed();
        }

        private static Builder NewBuilder(MessageDescriptor type)
        {
            return new Builder(type);
        }

        public object GetField(FieldDescriptor fd)
        {
            //TODO
            return fields.GetField(fd);
        }

        private DynamicMessage(MessageDescriptor type, FieldSet fields, UnknownFieldSet unknownFields)
        {
            this.type = type;
            this.fields = fields;
            this.unknownFields = unknownFields;
        }


        /// <summary>
        /// Builder for dynamic messages. Instances are created with DynamicMessage.CreateBuilder..
        /// </summary>
        public sealed partial class Builder : IMessage
        {

            private readonly MessageDescriptor type;
            private FieldSet fields;
            private UnknownFieldSet UnknownFields;

            internal Builder(MessageDescriptor type)
            {
                this.type = type;
                this.fields = FieldSet.CreateInstance();
                this.UnknownFields = new UnknownFieldSet();
            }



            public IDictionary<FieldDescriptor, object> AllFields
            {
                get { return fields.AllFieldDescriptors; }
            }

            /*public void AddRepeatedField(FieldDescriptor field, CodedInputStream input)
            {
                int fieldNumber = WireFormat.GetTagFieldNumber(input.ReadTag());
                FieldDescriptor fd = type.FindFieldByNumber(fieldNumber);
                VerifyContainingType(field);
                foreach (object elem in (List<object>) value)
                {
                    fields.AddRepeatedField(field, elem);
                }
            }*/

            public void SetField(FieldDescriptor fd, object value)
            {
                fields.SetField(fd, value);
                //AllFields.Add(fd, value);
            }



            private void VerifyContainingType(FieldDescriptor field)
            {
                if (field.ContainingType != type)
                {
                    throw new ArgumentException("FieldDescriptor does not match message type.");
                }
            }

            public bool IsInitialized
            {
                get { return fields.IsInitializedWithRespectTo(type.Fields.ByJsonName()); }
            }

            MessageDescriptor IMessage.Descriptor => throw new NotImplementedException();

            public DynamicMessage Build()
            {
                if (fields != null && !IsInitialized)
                {
                    throw new Exception(String.Format("Message {0} is missing required fields", new DynamicMessage(type, fields, UnknownFields).GetType()));
                }
                return BuildPartial();
            }

            public DynamicMessage BuildPartial()
            {
                if (fields == null)
                {
                    throw new InvalidOperationException("Build() has already been called on this Builder.");
                }
                fields.MakeImmutable();
                DynamicMessage result = new DynamicMessage(type, fields, UnknownFields);
                fields = null;
                UnknownFields = null;
                return result;
            }

            /*public Builder MergeFrom(ByteString data)
            {
                *//*CodedInputStream input = data.CreateCodedInput();
                MergeFrom(input);
                return null;*//*
            }*/

            public void MergeFrom(CodedInputStream input)
            {
                //input.ReadRawMessage(this);
                uint tag;
                while ((tag = input.ReadTag()) != 0)
                {
                    int fieldNumber = WireFormat.GetTagFieldNumber(tag);
                    var wireType = WireFormat.GetTagWireType(tag);
                    FieldDescriptor fd = type.FindFieldByNumber(fieldNumber);
                    //Console.WriteLine("Processing field:" + fd.FullName);

                    if (fd == null)
                    {
                        throw new Exception("Field descriptor not found for fieldNumber:" + fieldNumber);
                    }
                    if (fd.FieldType == FieldType.Message)
                    {
                        Console.WriteLine("Processing complex field started: " + fd.FullName + ", fieldNumber:" + fieldNumber + ", tag:" + tag);

                        Builder value = NewBuilder(fd.MessageType);
                        input.ReadMessage(value);
                        DynamicMessage res = value.Build();
                        if (fd.ToProto().Label != FieldDescriptorProto.Types.Label.Repeated)
                            SetField(fd, res);
                        else
                            fields.AddRepeatedField(fd, res);
                        Console.WriteLine("Processing complex field ended: " + fd.FullName + ", fieldNumber:" + fieldNumber + ", tag:" + tag);
                    }
                    /*else if (fd.FieldType == FieldType.Enum)
                    {

                    }*/
                    else
                    {

                        if (fd.ToProto().Label != FieldDescriptorProto.Types.Label.Repeated)
                        {
                            object value = ReadField(fd.FieldType, input);
                            Console.WriteLine("Processing primitive field:" + fd.FullName + ", fieldNumber:" + fieldNumber + ", tag:" + tag + ", value:" + value + ", Label:" + fd.ToProto().Label);
                            SetField(fd, value);
                        }
                        else if (fd.ToProto().Label == FieldDescriptorProto.Types.Label.Repeated)
                        {
                            Console.WriteLine("Processing primitive field:" + fd.FullName + ", fieldNumber:" + fieldNumber + ", tag:" + tag);
                            //RepeatedField rp = new Collections.RepeatedField();

                            //fields.AddRepeatedField(fd, value);
                            //input.
                            /*var fc = GetFieldCodec(tag, fd.FieldType);
                            RepeatedField<object> rf = new RepeatedField<object>();
                            rf.AddEntriesFrom(input, fc);
                            int cnt = rf.Count;
                            IEnumerator enumerator = rf.GetEnumerator();*/
                            IEnumerator enumerator = GetFieldCodec(tag, fd.FieldType, input);
                            while (enumerator.MoveNext())
                            {
                                Console.WriteLine(enumerator.Current);
                                fields.AddRepeatedField(fd, enumerator.Current);
                            }
                            //Console.WriteLine("1:" + ReadField(fd.FieldType, input));
                            //Console.WriteLine("2:" + ReadField(fd.FieldType, input));
                        }
                        else
                        {
                            UnknownFields = UnknownFieldSet.MergeFieldFrom(UnknownFields, input);
                        }
                    }

                }
                //input.ReadRawMessage(this);
                //fields.MergeFrom(input);
                //MessageExtensions.MergeFrom(input);
            }

            private static IEnumerator GetFieldCodec(uint tag, FieldType fieldType, CodedInputStream input)
            {
                switch (fieldType)
                {
                    case FieldType.Int32:
                        var fc = FieldCodec.ForInt32(tag);
                        RepeatedField<int> rf = new RepeatedField<int>();
                        rf.AddEntriesFrom(input, fc);
                        return rf.GetEnumerator();
                    case FieldType.Int64:
                        var fc1 = FieldCodec.ForInt64(tag);
                        RepeatedField<long> rf1 = new RepeatedField<long>();
                        rf1.AddEntriesFrom(input, fc1);
                        return rf1.GetEnumerator();
                    case FieldType.Bool:
                        FieldCodec<bool> fcBool = FieldCodec.ForBool(tag);
                        RepeatedField<bool> rfBool = new RepeatedField<bool>();
                        rfBool.AddEntriesFrom(input, fcBool);
                        return rfBool.GetEnumerator();
                    case FieldType.UInt32:
                        RepeatedField<uint> rfUint = new RepeatedField<uint>();
                        rfUint.AddEntriesFrom(input, FieldCodec.ForUInt32(tag));
                        return rfUint.GetEnumerator();
                    case FieldType.UInt64:
                        RepeatedField<ulong> rfUint64 = new RepeatedField<ulong>();
                        rfUint64.AddEntriesFrom(input, FieldCodec.ForUInt64(tag));
                        return rfUint64.GetEnumerator();
                    case FieldType.SInt32:
                        RepeatedField<int> rfSint32 = new RepeatedField<int>();
                        rfSint32.AddEntriesFrom(input, FieldCodec.ForSInt32(tag));
                        return rfSint32.GetEnumerator();
                    case FieldType.SInt64:
                        RepeatedField<long> rfSint64 = new RepeatedField<long>();
                        rfSint64.AddEntriesFrom(input, FieldCodec.ForSInt64(tag));
                        return rfSint64.GetEnumerator();
                    case FieldType.Fixed32:
                        RepeatedField<uint> rfFixed32 = new RepeatedField<uint>();
                        rfFixed32.AddEntriesFrom(input, FieldCodec.ForFixed32(tag));
                        return rfFixed32.GetEnumerator();
                    case FieldType.Fixed64:
                        RepeatedField<ulong> rfFixed64 = new RepeatedField<ulong>();
                        rfFixed64.AddEntriesFrom(input, FieldCodec.ForFixed64(tag));
                        return rfFixed64.GetEnumerator();
                    case FieldType.SFixed32:
                        RepeatedField<int> rfSFixed32 = new RepeatedField<int>();
                        rfSFixed32.AddEntriesFrom(input, FieldCodec.ForSFixed32(tag));
                        return rfSFixed32.GetEnumerator();
                    case FieldType.SFixed64:
                        RepeatedField<long> rfSFixed64 = new RepeatedField<long>();
                        rfSFixed64.AddEntriesFrom(input, FieldCodec.ForSFixed64(tag));
                        return rfSFixed64.GetEnumerator();
                    case FieldType.Float:
                        RepeatedField<float> rfFloat = new RepeatedField<float>();
                        rfFloat.AddEntriesFrom(input, FieldCodec.ForFloat(tag));
                        return rfFloat.GetEnumerator();
                    case FieldType.Double:
                        RepeatedField<double> rfDouble = new RepeatedField<double>();
                        rfDouble.AddEntriesFrom(input, FieldCodec.ForDouble(tag));
                        return rfDouble.GetEnumerator();
                    case FieldType.String:
                        RepeatedField<String> rfString = new RepeatedField<String>();
                        rfString.AddEntriesFrom(input, FieldCodec.ForString(tag));
                        return rfString.GetEnumerator();
                    case FieldType.Bytes:
                        RepeatedField<ByteString> rfBytes = new RepeatedField<ByteString>();
                        rfBytes.AddEntriesFrom(input, FieldCodec.ForBytes(tag));
                        return rfBytes.GetEnumerator();
                        /*case FieldType.Enum:
                            RepeatedField<Enum> rfEnum = new RepeatedField<Enum>();
                            rfEnum.AddEntriesFrom(input, FieldCodec.ForEnum(tag));
                            return rfEnum.GetEnumerator();*/

                }
                return null;
            }

            private object ReadField(FieldType fieldType, CodedInputStream input)
            {
                switch (fieldType)
                {
                    case FieldType.Int32:
                        return input.ReadInt32();
                    case FieldType.Int64:
                        return input.ReadInt64();
                    case FieldType.Bytes:
                        return input.ReadBytes();
                    case FieldType.String:
                        return input.ReadString();
                    case FieldType.Double:
                        return input.ReadDouble();
                    case FieldType.Float:
                        return input.ReadFloat();
                    case FieldType.Bool:
                        return input.ReadBool();
                    case FieldType.UInt32:
                        return input.ReadUInt32();
                    case FieldType.UInt64:
                        return input.ReadUInt64();
                    case FieldType.Fixed32:
                        return input.ReadFixed32();
                    case FieldType.Fixed64:
                        return input.ReadFixed64();
                    case FieldType.SFixed64:
                        return input.ReadSFixed64();
                    case FieldType.SFixed32:
                        return input.ReadSFixed32();
                    case FieldType.SInt32:
                        return input.ReadSInt32();
                    case FieldType.SInt64:
                        return input.ReadSInt64();
                    case FieldType.Enum:
                        return input.ReadEnum();
                        /*case FieldType.Message:
                            return input.ReadMessage();*/

                }
                return null;
            }

            /*private void MergeFrom(CodedInputStream input, ExtensionRegistry extensionRegistry)
            {
                throw new NotImplementedException();
            }*/

            void IMessage.WriteTo(CodedOutputStream output)
            {
                throw new NotImplementedException();
            }

            int IMessage.CalculateSize()
            {
                throw new NotImplementedException();
            }

            internal DynamicMessage BuildParsed()
            {

                /*if (type.GetOptions().MapEntry)
                {
                    foreach (FieldDescriptor field in type.Fields.InDeclarationOrder())
                    {
                        if (!fields.HasField(field))
                        {
                            if (field.FieldType == FieldType.Message)
                            {
                                DynamicMessage dm = new DynamicMessage(field.MessageType, fields, UnknownFields);
                                fields.SetField(field, dm);
                            }
                            else
                            {
                                fields.SetField(field, field.FieldType);
                            }

                        }

                    }
                }*/

                fields.MakeImmutable();
                DynamicMessage result = new DynamicMessage(type, fields, UnknownFields);
                return result;
            }

            public void AddRepeatedField(FieldDescriptor fd, object v)
            {
                fields.AddRepeatedField(fd, v);
            }
        }


    }

}
