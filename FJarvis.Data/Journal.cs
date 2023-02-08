using FJarvis.Data.Traits;
using Serilog;
using Serilog.Events;

namespace FJarvis.Data;

using Autofac;

// This is a class that 

public class Journal 
{ 
    private readonly IComponentContext _container;
    private static HashSet<TraitData>  _entities;

    public Journal(IComponentContext  container, ILogger logger)
    {
        // store the container
        _container = container;
        
        logger.Information("Journal Initialized");
        
    }
   
    // This is the entry point for the application to make amends to the Journal
    public void Append(LogEventLevel level, string message)
    {
        // Log the message
        _container.Resolve<ILogger>().Write(level, message);
    }

    
  
    public HashSet<TraitData> Entities => _entities;
}