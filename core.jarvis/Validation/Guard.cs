using core.jarvis.Exceptions;

namespace core.jarvis.Validation;

/// <summary>
/// Provides guard clauses for input validation.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throws a BusinessRuleException if the condition is true.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="message">The error message if the condition is true.</param>
    /// <exception cref="BusinessRuleException">Thrown when the condition is true.</exception>
    public static void Against(bool condition, string message)
    {
        if (condition)
            throw new BusinessRuleException("GUARD", message);
    }

    /// <summary>
    /// Throws an ArgumentNullException if the value is null.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
    public static void AgainstNull<T>(T? value, string paramName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Throws a ValidationException if the string is null or whitespace.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="ValidationException">Thrown when the value is null or whitespace.</exception>
    public static void AgainstEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException(paramName, $"{paramName} cannot be empty");
    }

    /// <summary>
    /// Throws a ValidationException if the GUID is empty.
    /// </summary>
    /// <param name="value">The GUID value to check.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="ValidationException">Thrown when the value is empty.</exception>
    public static void AgainstEmptyGuid(Guid value, string paramName)
    {
        if (value == Guid.Empty)
            throw new ValidationException(paramName, $"{paramName} cannot be empty");
    }

    /// <summary>
    /// Throws a ValidationException if the value is outside the specified range.
    /// </summary>
    /// <typeparam name="T">The type of the value (must be comparable).</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="ValidationException">Thrown when the value is outside the range.</exception>
    public static void AgainstOutOfRange<T>(T value, T min, T max, string paramName) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new ValidationException(paramName, $"{paramName} must be between {min} and {max}");
    }
}