using FJarvis.Data.Data;
using FJarvis.Data.Traits;
using Serilog;
using Serilog.Events;

namespace FJarvis.Data;

using Autofac;

// This is a class that 

public class Journal 
{ 
    private readonly IComponentContext _container;
    private readonly JournalLogger _journalLogger;
    private readonly EntityManager _entityManager;
    private readonly JournalInfo _journalInfo;

    public Journal(IComponentContext  container, ILogger logger, JournalLogger journalLogger,  EntityManager entityManager, JournalInfo journalInfo)
    {
        // store the container
        _container = container;
        _journalLogger = journalLogger;
        _journalInfo = journalInfo;
        _entityManager = entityManager;
        
        logger.Information("Journal Initialized");
        
    }
   
    // This is the entry point for the application to make amends to the Journal
    public void Append(LogEventLevel level, string message)
    {
        // Log the message
        _container.Resolve<ILogger>().Write(level, message);
    }

    public void Save()
    {
        // Check to see if Journal Logger has been initialized.
        if (_journalLogger == null)
            return;
        _journalLogger.Save();
    }

    /// <summary>
    /// Returns the Bitmask of each Entity
    /// </summary>
    /// <returns></returns>
    public HashSet<HeaderInfo> Headers()
    {
        // Return the Bitmask of each entity
        return _journalInfo.Headers;
    }

    
}