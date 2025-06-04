using core.jarvis.Data.GraphQL;

namespace core.jarvis.Exceptions
{
    /// <summary>
    /// Exception thrown when GraphQL operations fail
    /// </summary>
    public class GraphQLException : DomainException
    {
        /// <summary>
        /// GraphQL errors returned from the server
        /// </summary>
        public List<GraphQLError>? GraphQLErrors { get; }

        public GraphQLException(string message) : base("GRAPHQL_ERROR", message)
        {
        }

        public GraphQLException(string message, List<GraphQLError> errors) : base("GRAPHQL_ERROR", message, errors)
        {
            GraphQLErrors = errors;
        }

        public GraphQLException(string message, Exception innerException) : base("GRAPHQL_ERROR", message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when user is not authenticated
    /// </summary>
    public class UnauthorizedException : DomainException
    {
        public UnauthorizedException(string message) : base("UNAUTHORIZED", message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when user lacks required permissions
    /// </summary>
    public class ForbiddenException : DomainException
    {
        public ForbiddenException(string message) : base("FORBIDDEN", message)
        {
        }
    }
}