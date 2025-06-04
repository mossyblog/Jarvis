using core.jarvis.Data;
using core.jarvis.Validation;

namespace core.jarvis.tests.Examples.Blog;

/// <summary>
/// Extension methods for IDataContext to support blog domain operations.
/// Provides the desired fluent API: _dataContext.BlogHandler(blogId).GeneratePost()
/// </summary>
public static class DataContextBlogExtensions
{
    /// <summary>
    /// Gets a BlogHandler for the specified blog ID.
    /// Provides fluent API: _dataContext.BlogHandler(blogId).GeneratePost()
    /// </summary>
    /// <param name="dataContext">The data context instance</param>
    /// <param name="blogId">The blog entity ID</param>
    /// <returns>Initialized BlogHandler instance</returns>
    public static BlogHandler BlogHandler(this IDataContext dataContext, Guid blogId)
    {
        Guard.AgainstNull(dataContext, nameof(dataContext));
        Guard.AgainstEmptyGuid(blogId, nameof(blogId));

        // Use the existing For<> method - much cleaner!
        return dataContext.For<BlogHandler>(blogId);
    }
} 