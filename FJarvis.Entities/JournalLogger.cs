using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace FJarvis.Data;

public class JournalLogger : ILogEventEnricher
{
    private static readonly HashSet<LogEvent> _logEvents = new HashSet<LogEvent>();

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        Debug.WriteLine($"Logging Called {logEvent.MessageTemplate.Text}");
        _logEvents.Add(logEvent);
    }

    public void Save()
    {
        foreach (var logEvent in _logEvents)
        {
            Debug.WriteLine($"Saving {logEvent.MessageTemplate.Text}");
        }
    }
}