using FJarvis.Data.Traits;
using Serilog;
using Serilog.Events;

namespace FJarvis.Data;

using Autofac;

// This is a class that 

public class Journal 
{ 
    private readonly IComponentContext _container;
    private static HashSet<EntityInfo>  _entities;
    private readonly JournalLogger _journalLogger;

    public Journal(IComponentContext  container, ILogger logger, JournalLogger journalLogger)
    {
        // store the container
        _container = container;
        _journalLogger = journalLogger;
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
    
  
    public HashSet<EntityInfo> Entities => _entities;
}