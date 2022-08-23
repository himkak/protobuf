//using Google.ProtocolBuffers.Descriptors;

using System;
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
            throw new NotImplementedException();
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

        private DynamicMessage(MessageDescriptor type, FieldSet fields, UnknownFieldSet unknownFields)
        {
            this.type = type;
            this.fields = fields;
            this.unknownFields = unknownFields;
        }


        /// <summary>
        /// Builder for dynamic messages. Instances are created with DynamicMessage.CreateBuilder..
        /// </summary>
        public sealed partial class Builder
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

            public Builder AddRepeatedField(FieldDescriptor field, object value)
            {
                VerifyContainingType(field);
                fields.AddRepeatedField(field, value);
                return this;
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

        }


    }

}
