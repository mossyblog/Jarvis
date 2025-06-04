namespace core.jarvis.Exceptions;

public class EventEmissionException : DomainException
{
    public EventEmissionException(string message) : base("EVENT_EMISSION_FAILED", message)
    {
    }

    public EventEmissionException(string message, Exception innerException) : base("EVENT_EMISSION_FAILED", message, innerException)
    {
    }
}