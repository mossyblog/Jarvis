using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace core.jarvis.api.Services;

/// <summary>
/// Service for enforcing password policy and security requirements.
/// </summary>
public interface IPasswordPolicyService
{
    PasswordValidationResult ValidatePassword(string password, string? email = null);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

public class PasswordPolicyService : IPasswordPolicyService
{
    // Password requirements
    private const int MinimumLength = 8;
    private const int MinimumUniqueCharacters = 5;
    private const int MaximumLength = 128;
    
    // Common weak passwords to reject
    private static readonly HashSet<string> CommonWeakPasswords = new()
    {
        "password", "password123", "123456", "12345678", "qwerty", "abc123",
        "monkey", "letmein", "trustno1", "dragon", "baseball", "111111",
        "iloveyou", "master", "sunshine", "ashley", "bailey", "shadow",
        "123123", "654321", "superman", "qazwsx", "michael", "football",
        "p@ssw0rd", "passw0rd", "p@ssword", "pa$$w0rd", "pa$$word"
    };

    // Regex patterns for character requirements
    private static readonly Regex UppercasePattern = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowercasePattern = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex DigitPattern = new(@"\d", RegexOptions.Compiled);
    private static readonly Regex SpecialCharPattern = new(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]", RegexOptions.Compiled);
    private static readonly Regex RepeatingCharPattern = new(@"(.)\1{2,}", RegexOptions.Compiled);
    // Only check for longer sequential patterns (4+ characters) to avoid being too strict
    private static readonly Regex SequentialPattern = new(@"(0123|1234|2345|3456|4567|5678|6789|7890|abcd|bcde|cdef|defg|efgh|fghi|ghij|hijk|ijkl|jklm|klmn|lmno|mnop|nopq|opqr|pqrs|qrst|rstu|stuv|tuvw|uvwx|vwxy|wxyz|qwerty|asdf|zxcv)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public PasswordValidationResult ValidatePassword(string password, string? email = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required");
            return new PasswordValidationResult { IsValid = false, Errors = errors, Score = 0 };
        }

        // Length checks
        if (password.Length < MinimumLength)
        {
            errors.Add($"Password must be at least {MinimumLength} characters long");
        }

        if (password.Length > MaximumLength)
        {
            errors.Add($"Password must not exceed {MaximumLength} characters");
        }

        // Character variety requirements
        var hasUppercase = UppercasePattern.IsMatch(password);
        var hasLowercase = LowercasePattern.IsMatch(password);
        var hasDigit = DigitPattern.IsMatch(password);
        var hasSpecialChar = SpecialCharPattern.IsMatch(password);

        var characterTypes = 0;
        if (hasUppercase) characterTypes++;
        if (hasLowercase) characterTypes++;
        if (hasDigit) characterTypes++;
        if (hasSpecialChar) characterTypes++;

        if (characterTypes < 3)
        {
            errors.Add("Password must contain at least 3 of the following: uppercase letters, lowercase letters, numbers, special characters");
        }

        // Check for common patterns
        if (RepeatingCharPattern.IsMatch(password))
        {
            errors.Add("Password must not contain more than 2 repeating characters in a row");
        }

        if (SequentialPattern.IsMatch(password))
        {
            errors.Add("Password must not contain sequential characters (e.g., 123, abc)");
        }

        // Check unique characters
        var uniqueChars = password.Distinct().Count();
        if (uniqueChars < MinimumUniqueCharacters)
        {
            errors.Add($"Password must contain at least {MinimumUniqueCharacters} unique characters");
        }

        // Check against common passwords
        if (CommonWeakPasswords.Contains(password.ToLowerInvariant()))
        {
            errors.Add("This password is too common. Please choose a more unique password");
        }

        // Check if password contains email/username
        if (!string.IsNullOrEmpty(email))
        {
            var emailParts = email.Split('@', '.');
            foreach (var part in emailParts.Where(p => p.Length > 3))
            {
                if (password.Contains(part, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Password must not contain parts of your email address");
                    break;
                }
            }
        }

        // Calculate password strength score (0-100)
        var score = CalculatePasswordScore(password, hasUppercase, hasLowercase, hasDigit, hasSpecialChar, uniqueChars);

        // Require minimum score - lowered threshold for reasonable passwords
        if (score < 50)
        {
            errors.Add("Password is too weak. Please choose a stronger password");
        }

        return new PasswordValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Score = score,
            Strength = GetPasswordStrength(score)
        };
    }

    private int CalculatePasswordScore(string password, bool hasUpper, bool hasLower, bool hasDigit, bool hasSpecial, int uniqueChars)
    {
        var score = 0;

        // Length score (up to 30 points)
        score += Math.Min(30, password.Length * 2);

        // Character variety (up to 40 points)
        if (hasUpper) score += 10;
        if (hasLower) score += 10;
        if (hasDigit) score += 10;
        if (hasSpecial) score += 10;

        // Unique characters (up to 20 points)
        score += Math.Min(20, uniqueChars * 2);

        // Entropy bonus (up to 10 points)
        var entropy = CalculateEntropy(password);
        score += Math.Min(10, (int)(entropy / 5));

        return Math.Min(100, score);
    }

    private double CalculateEntropy(string password)
    {
        var charCounts = password.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());
        var length = password.Length;
        var entropy = 0.0;

        foreach (var count in charCounts.Values)
        {
            var probability = (double)count / length;
            entropy -= probability * Math.Log2(probability);
        }

        return entropy * length;
    }

    private PasswordStrength GetPasswordStrength(int score)
    {
        return score switch
        {
            >= 80 => PasswordStrength.VeryStrong,
            >= 60 => PasswordStrength.Strong,
            >= 40 => PasswordStrength.Medium,
            >= 20 => PasswordStrength.Weak,
            _ => PasswordStrength.VeryWeak
        };
    }

    public string HashPassword(string password)
    {
        // Use BCrypt with a cost factor of 12 (good balance of security and performance)
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // Invalid hash format
            return false;
        }
    }
}

public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public int Score { get; set; }
    public PasswordStrength Strength { get; set; }
}

public enum PasswordStrength
{
    VeryWeak,
    Weak,
    Medium,
    Strong,
    VeryStrong
}