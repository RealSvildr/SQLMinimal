using System;
using System.Runtime.Serialization;

namespace SqlMinimal {
    [Serializable]
    internal class SqlMinimalException : Exception {
        public SqlMinimalException() { }
        public SqlMinimalException(string message) : base(message) { }
        public SqlMinimalException(string message, Exception innerException) : base(message, innerException) { }
        protected SqlMinimalException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}