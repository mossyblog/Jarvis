using Xunit;

namespace core.jarvis.tests.Helpers
{
    /// <summary>
    /// Test collection for tests that must run sequentially to avoid interference.
    /// This is used for tests that query shared state like audit events.
    /// </summary>
    [CollectionDefinition("Sequential", DisableParallelization = true)]
    public class SequentialTestCollection
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and configure sequential execution.
    }
}