namespace core.jarvis.Exceptions
{
    /// <summary>
    /// Exception thrown when an optimistic concurrency conflict is detected during data persistence.
    /// This typically indicates that the data being saved was modified by another process
    /// since it was originally read.
    /// </summary>
    [Serializable] 
    public class ConcurrencyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
        /// </summary>
        public ConcurrencyException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyException"/> class
        /// with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ConcurrencyException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyException"/> class
        /// with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ConcurrencyException(string message, Exception inner) : base(message, inner) { }

    }
} 