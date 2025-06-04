namespace core.jarvis.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a required component is not found on an entity.
    /// </summary>
    public class ComponentNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentNotFoundException"/> class.
        /// </summary>
        public ComponentNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ComponentNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentNotFoundException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ComponentNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentNotFoundException"/> class with details about the missing component and entity.
        /// </summary>
        /// <param name="componentTypeName">The name of the component type that was not found.</param>
        /// <param name="entityId">The ID of the entity where the component was expected.</param>
        public ComponentNotFoundException(string componentTypeName, Guid entityId)
            : base($"Component of type '{componentTypeName}' was not found on entity with ID '{entityId}'.")
        {
            ComponentTypeName = componentTypeName;
            EntityId = entityId;
        }

        /// <summary>
        /// Gets the name of the component type that was not found.
        /// </summary>
        public string? ComponentTypeName { get; }

        /// <summary>
        /// Gets the ID of the entity where the component was expected.
        /// </summary>
        public Guid? EntityId { get; }
    }
}