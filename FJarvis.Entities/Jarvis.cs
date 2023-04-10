using Autofac;
using FJarvis.Data.Traits;
using Serilog;

namespace FJarvis.Data;

public static class Jarvis
{
    private static IContainer container;

    public static void Initialize()
    {
        var builder = new ContainerBuilder();
        var journalLogger = new JournalLogger();
        var logger = new LoggerConfiguration().Enrich.With(journalLogger).CreateLogger();
        
        // Register dependencies
        builder.RegisterInstance(logger).As<ILogger>();

        // Register the Entity class
        builder.RegisterType<EntityInfo>().AsSelf().InstancePerLifetimeScope();
        builder.RegisterInstance(journalLogger).AsSelf().SingleInstance();
        builder.RegisterType<EntityManager>().AsSelf().SingleInstance();

        // Register the Journal class
        builder.RegisterType<Journal>().AsSelf().SingleInstance();

        // Build the container
        container = builder.Build();
        container.Resolve<EntityInfo>();
    }
    
    
    public static Journal Journal()
    {
        return container.Resolve<Journal>();
    }
    
    public static EntityManager EntityManager()
    {
        return container.Resolve<EntityManager>();
    }
}