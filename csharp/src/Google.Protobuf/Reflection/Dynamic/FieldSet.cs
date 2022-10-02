using System;
using System.Collections;
using System.Collections.Generic;
using Type = Google.Protobuf.WellKnownTypes.Type;

namespace Google.Protobuf.Reflection.Dynamic
{

    public interface IFieldDescriptorLite : IComparable<IFieldDescriptorLite>
    {
        bool IsRepeated { get; }
        bool IsRequired { get; }
        bool IsPacked { get; }
        bool IsExtension { get; }
        bool MessageSetWireFormat { get; }
        int FieldNumber { get; }
        string Name { get; }
        string FullName { get; }
        //IEnumLiteMap EnumType { get; }
        FieldType FieldType { get; }
        Type MappedType { get; }
        //MappedType MappedType { get; }
        object DefaultValue { get; }
    }

    internal sealed class FieldSet
    {

        private IDictionary<FieldDescriptor, object> fields;

        public static FieldSet CreateInstance()
        {
            // Use SortedList to keep fields in the canonical order
            return new FieldSet(new SortedList<FieldDescriptor, object>());
        }

        private FieldSet(IDictionary<FieldDescriptor, object> fields)
        {
            this.fields = fields;
        }



        /// <summary>
        /// Force coercion to full descriptor dictionary.
        /// </summary>
        internal IDictionary<FieldDescriptor, object> AllFieldDescriptors
        {
            get
            {
                SortedList<FieldDescriptor, object> copy =
                    new SortedList<FieldDescriptor, object>();
                foreach (KeyValuePair<FieldDescriptor, object> fd in fields)
                {
                    FieldDescriptor fieldDesc = fd.Key;
                    copy.Add(fieldDesc, fd.Value);
                }
                //return Dictionaries.AsReadOnly(copy);
                fields = copy;
                return fields;
            }
        }



        internal bool IsInitialized
        {
            get
            {
                foreach (KeyValuePair<FieldDescriptor, object> entry in fields)
                {

                }
                return true;
            }
        }

        internal bool IsInitializedWithRespectTo(IEnumerable typeFields)
        {
            foreach (KeyValuePair<string, FieldDescriptor> field in typeFields)
            {
                if (field.Value.IsRequired && !HasField(field.Value))
                {
                    return false;
                }
            }
            return IsInitialized;
        }

        /// <summary>
        /// See <see cref="IMessageLite.HasField"/>.
        /// </summary>
        public bool HasField(FieldDescriptor field)
        {
            if (field.IsRepeated)
            {
                throw new ArgumentException("HasField() can only be called on non-repeated fields.");
            }

            return fields.ContainsKey(field);
        }

        /// <summary>
        /// Makes this FieldSet immutable, and returns it for convenience. Any
        /// mutable repeated fields are made immutable, as well as the map itself.
        /// </summary>
        internal FieldSet MakeImmutable()
        {
            // First check if we have any repeated values
            bool hasRepeats = false;
            foreach (object value in fields.Values)
            {
                IList<object> list = value as IList<object>;
                if (list != null && !list.IsReadOnly)
                {
                    hasRepeats = true;
                    break;
                }
            }

            return this;
        }

        internal int getSerializedSize()
        {
            int size = 0;
            foreach (KeyValuePair<FieldDescriptor, object> kvPair in fields)
            {
                size += computeSize(kvPair.Key, kvPair.Value);
            }
            return size;
        }

        private int computeSize(FieldDescriptor desc, object value)
        {
            if (desc.IsRepeated)
            {
                int dataSize = 0;
                int tagSize = CodedOutputStream.ComputeTagSize(desc.FieldNumber);
                foreach (object val in (IEnumerable) value)
                {
                    dataSize += ComputeElementSizeNoTag(desc.FieldType, val) + tagSize;
                }
                return dataSize;
            }
            else
            {
                return ComputeElementSize(desc.FieldType, desc.FieldNumber, value);
            }

        }

        private int ComputeElementSize(FieldType fieldType, int fieldNumber, object value)
        {
            int tagSize = CodedOutputStream.ComputeTagSize(fieldNumber);
            return tagSize + ComputeElementSizeNoTag(fieldType, value);
        }

        private int ComputeElementSizeNoTag(FieldType fieldType, object value)
        {
            switch (fieldType)
            {
                case FieldType.String:
                    return CodedOutputStream.ComputeStringSize(value.ToString());
                case FieldType.Bool:
                    return CodedOutputStream.ComputeBoolSize(bool.Parse(value.ToString()));
                case FieldType.Int32:
                    return CodedOutputStream.ComputeInt32Size(int.Parse(value.ToString()));
                case FieldType.Int64:
                    return CodedOutputStream.ComputeInt64Size(long.Parse(value.ToString()));
                case FieldType.Double:
                    return CodedOutputStream.ComputeDoubleSize(double.Parse(value.ToString()));
                case FieldType.Float:
                    return CodedOutputStream.ComputeFloatSize(float.Parse(value.ToString()));
                case FieldType.Bytes:
                    return CodedOutputStream.ComputeBytesSize((ByteString) value);
                case FieldType.UInt32:
                    return CodedOutputStream.ComputeUInt32Size(uint.Parse(value.ToString()));
                case FieldType.UInt64:
                    return CodedOutputStream.ComputeUInt64Size(ulong.Parse(value.ToString()));
                case FieldType.SInt32:
                    return CodedOutputStream.ComputeSInt32Size(int.Parse(value.ToString()));
                case FieldType.SInt64:
                    return CodedOutputStream.ComputeSInt64Size(long.Parse(value.ToString()));
                case FieldType.Fixed32:
                    return CodedOutputStream.ComputeFixed32Size(uint.Parse(value.ToString()));
                case FieldType.Fixed64:
                    return CodedOutputStream.ComputeFixed64Size(ulong.Parse(value.ToString()));
                case FieldType.SFixed32:
                    return CodedOutputStream.ComputeSFixed32Size(int.Parse(value.ToString()));
                case FieldType.SFixed64:
                    return CodedOutputStream.ComputeSFixed64Size(long.Parse(value.ToString()));
                case FieldType.Message:
                    return CodedOutputStream.ComputeMessageSize((IMessage) value);
                case FieldType.Enum:
                    return CodedOutputStream.ComputeEnumSize((int) value);

            }
            throw new ArgumentException("unidentified type :" + fieldType.ToString());
        }


        public void WriteTo(CodedOutputStream output)
        {
            foreach (KeyValuePair<FieldDescriptor, object> kvPair in fields)
            {
                WriteField(kvPair.Key, kvPair.Value, output);
            }
        }

        private void WriteField(FieldDescriptor key, object value, CodedOutputStream output)
        {
            FieldType fieldType = key.FieldType;
            if (key.IsRepeated)
            {

                foreach (Object val in (IEnumerable) value)
                {
                    WriteElement(output, fieldType, key.FieldNumber, val);
                }
            }
            else
            {
                WriteElement(output, fieldType, key.FieldNumber, value);
            }
        }

        private void WriteElement(CodedOutputStream output, FieldType type, int number, object value)
        {
            output.WriteTag(number, GetWireFormatForFieldType(type));
            WriteElementNoTag(output, type, value);
        }

        private WireFormat.WireType GetWireFormatForFieldType(FieldType type)
        {
            switch (type)
            {
                case FieldType.Float:
                    return WireFormat.WireType.Fixed32;
                case FieldType.Fixed32:
                    return WireFormat.WireType.Fixed32;
                case FieldType.SFixed32:
                    return WireFormat.WireType.Fixed32;
                case FieldType.Fixed64:
                    return WireFormat.WireType.Fixed64;
                case FieldType.SFixed64:
                    return WireFormat.WireType.Fixed64;
                case FieldType.Double:
                    return WireFormat.WireType.Fixed64;
                case FieldType.Bool:
                    return WireFormat.WireType.Varint;
                case FieldType.Int32:
                    return WireFormat.WireType.Varint;
                case FieldType.Int64:
                    return WireFormat.WireType.Varint;
                case FieldType.UInt32:
                    return WireFormat.WireType.Varint;
                case FieldType.UInt64:
                    return WireFormat.WireType.Varint;
                case FieldType.Enum:
                    return WireFormat.WireType.Varint;
                case FieldType.SInt32:
                    return WireFormat.WireType.Varint;
                case FieldType.SInt64:
                    return WireFormat.WireType.Varint;
                case FieldType.String:
                    return WireFormat.WireType.LengthDelimited;
                case FieldType.Bytes:
                    return WireFormat.WireType.LengthDelimited;
                case FieldType.Message:
                    return WireFormat.WireType.LengthDelimited;
                case FieldType.Group:
                    return WireFormat.WireType.StartGroup;
            }
            throw new ArgumentException("unidentified type :" + type.ToString());
        }

        private void WriteElementNoTag(CodedOutputStream output, FieldType type, object value)
        {
            Console.WriteLine("type:" + type.ToString() + ", value:" + value);
            switch (type)
            {
                case FieldType.String:
                    output.WriteString(value.ToString());
                    return;
                case FieldType.Bool:
                    output.WriteBool(Boolean.Parse(value.ToString()));
                    return;
                case FieldType.Int32:
                    output.WriteInt32((int) value);
                    return;
                case FieldType.Int64:
                    output.WriteInt64((long) value);
                    return;
                case FieldType.Double:
                    output.WriteDouble((double) value);
                    return;
                case FieldType.UInt32:
                    output.WriteUInt32((uint) value);
                    return;
                case FieldType.UInt64:
                    output.WriteUInt64((ulong) value);
                    return;
                case FieldType.SInt32:
                    output.WriteSInt32((int) value);
                    return;
                case FieldType.SInt64:
                    output.WriteSInt64((long) value);
                    return;
                case FieldType.Fixed32:
                    output.WriteFixed32((uint) value);
                    return;
                case FieldType.Fixed64:
                    output.WriteFixed64((ulong) value);
                    return;
                case FieldType.SFixed32:
                    output.WriteSFixed32((int) value);
                    return;
                case FieldType.SFixed64:
                    output.WriteSFixed64((long) value);
                    return;
                case FieldType.Float:
                    output.WriteFloat((float) value);
                    return;
                case FieldType.Bytes:
                    output.WriteBytes((ByteString) value);
                    return;
                case FieldType.Enum:
                    output.WriteEnum((int) value);
                    return;
                case FieldType.Message:
                    output.WriteMessage((IMessage) value);
                    return;
            }
            throw new ArgumentException("unidentified type :" + type.ToString());
        }

        internal void SetField(FieldDescriptor field, object value)
        {
            fields.Add(field, value);
        }

        internal void AddRepeatedField(FieldDescriptor field, object value)
        {
            if (!field.IsRepeated)
            {
                throw new ArgumentException("AddRepeatedField can only be called on repeated fields.");
            }
            //VerifyType(field, value);
            // TODO what about the elements already present in the list
            if (!fields.TryGetValue(field, out object list))
            {
                list = new List<object>();
                fields[field] = list;
            }

            ((IList<object>) list).Add(value);
        }

        internal object GetField(FieldDescriptor fd)
        {
            fields.TryGetValue(fd, out object value);
            return value;
        }

        /*internal void setField(FieldDescriptor field, FieldType fieldType)
        {
            throw new NotImplementedException();
        }*/
    }

}
