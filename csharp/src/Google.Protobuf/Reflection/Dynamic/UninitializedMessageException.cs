using System;
using System.Collections.Generic;
using System.Text;

namespace Google.Protobuf.Reflection.Dynamic
{
    public sealed class UninitializedMessageException : Exception
    {
        private readonly IList<string> missingFields;

        private UninitializedMessageException(IList<string> missingFields)
            : base(BuildDescription(missingFields))
        {
            this.missingFields = new List<string>(missingFields);
        }

        /// <summary>
        /// Returns a read-only list of human-readable names of
        /// required fields missing from this message. Each name
        /// is a full path to a field, e.g. "foo.bar[5].baz"
        /// </summary>
        public IList<string> MissingFields
        {
            get { return missingFields; }
        }

        /// <summary>
        /// Converts this exception into an InvalidProtocolBufferException.
        /// When a parsed message is missing required fields, this should be thrown
        /// instead of UninitializedMessageException.
        /// </summary>
        public InvalidProtocolBufferException AsInvalidProtocolBufferException()
        {
            return new InvalidProtocolBufferException(Message);
        }

        /// <summary>
        /// Constructs the description string for a given list of missing fields.
        /// </summary>
        private static string BuildDescription(IEnumerable<string> missingFields)
        {
            StringBuilder description = new StringBuilder("Message missing required fields: ");
            bool first = true;
            foreach (string field in missingFields)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    description.Append(", ");
                }
                description.Append(field);
            }
            return description.ToString();
        }

        /// <summary>
        /// For Lite exceptions that do not known how to enumerate missing fields
        /// </summary>
        public UninitializedMessageException(DynamicMessage message)
            : base(String.Format("Message {0} is missing required fields", message.GetType()))
        {
            missingFields = new List<string>();
        }



    }
}
