using System.Text;

namespace core.jarvis.data
{
    /// <summary>
    /// String extension for converting PascalCase to snake_case, used for mapping C# property names to PostgreSQL columns.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Converts a PascalCase or camelCase string to snake_case.
        /// Handles acronyms (XML, API, etc.) and ID suffixes properly.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <returns>snake_case string.</returns>
        public static string ToSnakeCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            var result = new StringBuilder();
            
            for (int i = 0; i < str.Length; i++)
            {
                var currentChar = str[i];
                
                if (i == 0)
                {
                    // First character is always lowercase
                    result.Append(char.ToLower(currentChar));
                }
                else if (char.IsUpper(currentChar))
                {
                    // Check if this is part of an acronym (multiple uppercase letters in a row)
                    bool isAcronym = i > 0 && char.IsUpper(str[i - 1]);
                    bool isLastCharOfAcronym = i < str.Length - 1 && !char.IsUpper(str[i + 1]);
                    
                    // Add underscore before uppercase letter if:
                    // 1. Previous character is lowercase or digit
                    // 2. This is the last character of an acronym (e.g., XMLParser -> xml_parser)
                    if ((i > 0 && (char.IsLower(str[i - 1]) || char.IsDigit(str[i - 1]))) ||
                        (isAcronym && isLastCharOfAcronym && i < str.Length - 1))
                    {
                        result.Append('_');
                    }
                    
                    result.Append(char.ToLower(currentChar));
                }
                else
                {
                    result.Append(currentChar);
                }
            }
            
            return result.ToString();
        }
    }
}