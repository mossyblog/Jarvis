using System;
using System.Text;

public static class StringExtensions
{
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
                result.Append(char.ToLower(currentChar));
            }
            else if (char.IsUpper(currentChar))
            {
                bool isAcronym = i > 0 && char.IsUpper(str[i - 1]);
                bool isLastCharOfAcronym = i < str.Length - 1 && !char.IsUpper(str[i + 1]);
                
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

class Program 
{
    static void Main()
    {
        Console.WriteLine("BlogPostComponent -> " + "BlogPostComponent".ToSnakeCase());
        Console.WriteLine("TestComponent -> " + "TestComponent".ToSnakeCase());
    }
}