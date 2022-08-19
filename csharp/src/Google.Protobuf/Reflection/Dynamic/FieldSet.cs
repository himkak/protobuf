using Google.Protobuf.WellKnownTypes;
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
        bool MessageSetWireFormat { get; } //field.ContainingType.Options.MessageSetWireFormat
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
            //SortedList<IFieldDescriptorLite, object> sl = new SortedList<IFieldDescriptorLite, object>();
            // Use SortedList to keep fields in the canonical order
            return new FieldSet(new SortedList<FieldDescriptor, object>());
        }

        private FieldSet(IDictionary<FieldDescriptor, object> fields)
        {
            this.fields = fields;
        }

        /* public void MergeFrom(MessageDescriptor other)
         {
             foreach (KeyValuePair<string, FieldDescriptor> fd in other.Fields.ByJsonName())
             {
                 MergeField(fd.Key, fd.Value);
             }
         }

         private void MergeField(object mergeValue, FieldDescriptor field)
         {
             object existingValue;
             fields.TryGetValue(field, out existingValue);
             if (field.IsRepeated)
             {
                 if (existingValue == null)
                 {
                     existingValue = new List<object>();
                     fields[field] = existingValue;
                 }
                 IList<object> list = (IList<object>) existingValue;
                 foreach (object otherValue in (IEnumerable) mergeValue)
                 {
                     list.Add(otherValue);
                 }
             }
             else if (field.FieldType == FieldType.Message && existingValue != null)
             {
                 IMessage existingMessage = (IMessage) existingValue;
                 IMessag merged = existingMessage.WeakToBuilder()
                     .WeakMergeFrom((IMessageLite) mergeValue)
                     .WeakBuild();
                 this[field] = merged;
             }
             else
             {
                 this[field] = mergeValue;
             }
         }*/

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

        internal void AddRepeatedField(FieldDescriptor field, object value)
        {
            if (!field.IsRepeated)
            {
                throw new ArgumentException("AddRepeatedField can only be called on repeated fields.");
            }
            //VerifyType(field, value);
            object list;
            if (!fields.TryGetValue(field, out list))
            {
                list = new List<object>();
                fields[field] = list;
            }
            ((IList<object>) list).Add(value);
        }

        internal bool IsInitialized
        {
            get
            {
                foreach (KeyValuePair<FieldDescriptor, object> entry in fields)
                {
                    /*IFieldDescriptorLite field = entry.Key;
                    if (field.MappedType == Type.Message)
                    {
                        if (field.IsRepeated)
                        {
                            foreach (IMessageLite message in (IEnumerable) entry.Value)
                            {
                                if (!message.IsInitialized)
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            if (!((IMessageLite) entry.Value).IsInitialized)
                            {
                                return false;
                            }
                        }
                    }*/
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

            /*if (hasRepeats)
            {
                var tmp = new SortedList<IFieldDescriptorLite, object>();
                foreach (KeyValuePair<IFieldDescriptorLite, object> entry in fields)
                {
                    IList<object> list = entry.Value as IList<object>;
                    tmp[entry.Key] = list == null ? entry.Value : Lists.AsReadOnly(list);
                }
                fields = tmp;
            }*/

            //fields = Dictionaries.AsReadOnly(fields);

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
            return computeElementSize(desc.FieldType, desc.FieldNumber, value);
        }

        private int computeElementSize(FieldType fieldType, int fieldNumber, object value)
        {
            int tagSize = CodedOutputStream.ComputeTagSize(fieldNumber);
            return tagSize + ComputeElementSizeNoTag(fieldType, value);
        }

        private int ComputeElementSizeNoTag(FieldType fieldType, object value)
        {
            switch (fieldType)
            {
                case FieldType.String:
                    return CodedOutputStream.ComputeStringSize(((Value) value).StringValue);
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
            WriteElement(output, WireFormat.WireType.LengthDelimited, key.FieldNumber, value);

        }

        private void WriteElement(CodedOutputStream output, WireFormat.WireType type, int number, object value)
        {
            output.WriteTag(number, WireFormat.WireType.LengthDelimited);
            WriteElementNoTag(output, type, value);
        }

        private void WriteElementNoTag(CodedOutputStream output, WireFormat.WireType type, object value)
        {
            switch (type)
            {
                case WireFormat.WireType.LengthDelimited:
                    output.WriteString(((Value) value).StringValue);
                    return;

            }
            throw new ArgumentException("unidentified type :" + type.ToString());
        }
    }

}
