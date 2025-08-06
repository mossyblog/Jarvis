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
    /// <summary>
    /// Validates a password against the security policy requirements.
    /// Checks length, complexity, character variety, and common patterns.
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <param name="email">Optional email to check for password similarity</param>
    /// <returns>PasswordValidationResult indicating if password is valid and any specific errors</returns>
    PasswordValidationResult ValidatePassword(string password, string? email = null);
    
    /// <summary>
    /// Securely hashes a password using BCrypt with appropriate salt and work factor.
    /// </summary>
    /// <param name="password">The plain text password to hash</param>
    /// <returns>BCrypt hash string suitable for secure storage</returns>
    string HashPassword(string password);
    
    /// <summary>
    /// Verifies a plain text password against a BCrypt hash using constant-time comparison.
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hash">The stored BCrypt hash to verify against</param>
    /// <returns>True if the password matches the hash; false otherwise</returns>
    bool VerifyPassword(string password, string hash);
}

public class PasswordPolicyService : IPasswordPolicyService
{
    // Password Policy Configuration
    /// <summary>
    /// Minimum required password length for security compliance.
    /// Industry standard minimum length for strong passwords.
    /// </summary>
    private const int MinimumLength = 8;
    
    /// <summary>
    /// Minimum number of unique characters required in a password.
    /// Helps prevent passwords with excessive repetition.
    /// </summary>
    private const int MinimumUniqueCharacters = 5;
    
    /// <summary>
    /// Maximum allowed password length to prevent memory exhaustion attacks.
    /// Reasonable upper bound for password length.
    /// </summary>
    private const int MaximumLength = 128;
    
    /// <summary>
    /// Minimum number of different character types required (upper, lower, digit, special).
    /// Ensures password complexity for better security.
    /// </summary>
    private const int MinimumCharacterTypes = 3;
    
    /// <summary>
    /// BCrypt cost factor for password hashing.
    /// Balances security and performance for password verification.
    /// </summary>
    private const int BCryptCostFactor = 12;
    
    /// <summary>
    /// Minimum password strength score required for acceptance.
    /// Ensures passwords meet basic security requirements.
    /// </summary>
    private const int MinimumPasswordScore = 50;
    
    // Password Scoring Configuration
    /// <summary>
    /// Maximum points awarded for password length component.
    /// Ensures length contributes to but doesn't dominate score.
    /// </summary>
    private const int MaxLengthScore = 30;
    
    /// <summary>
    /// Points per character for length calculation.
    /// Rewards longer passwords without excessive weight.
    /// </summary>
    private const int PointsPerCharacter = 2;
    
    /// <summary>
    /// Points awarded for each character type present (upper, lower, digit, special).
    /// Encourages diverse character usage.
    /// </summary>
    private const int CharacterTypePoints = 10;
    
    /// <summary>
    /// Maximum points awarded for unique character diversity.
    /// Rewards character variety while capping contribution.
    /// </summary>
    private const int MaxUniqueCharScore = 20;
    
    /// <summary>
    /// Points per unique character for diversity calculation.
    /// Balances uniqueness importance in overall score.
    /// </summary>
    private const int PointsPerUniqueChar = 2;
    
    /// <summary>
    /// Maximum points awarded for password entropy.
    /// Caps entropy contribution to prevent skewing scores.
    /// </summary>
    private const int MaxEntropyScore = 10;
    
    /// <summary>
    /// Entropy divisor for calculating entropy points.
    /// Calibrates entropy impact on overall score.
    /// </summary>
    private const int EntropyDivisor = 5;
    
    // Password Strength Thresholds
    /// <summary>
    /// Minimum score threshold for "Strong" password classification.
    /// Industry-aligned threshold for secure passwords.
    /// </summary>
    private const int StrongPasswordThreshold = 60;
    
    /// <summary>
    /// Minimum score threshold for "Very Strong" password classification.
    /// Highest security tier for critical applications.
    /// </summary>
    private const int VeryStrongPasswordThreshold = 80;
    
    /// <summary>
    /// Minimum score threshold for "Medium" password classification.
    /// Acceptable baseline for moderate security requirements.
    /// </summary>
    private const int MediumPasswordThreshold = 40;
    
    /// <summary>
    /// Minimum score threshold for "Weak" password classification.
    /// Basic threshold above very weak passwords.
    /// </summary>
    private const int WeakPasswordThreshold = 20;
    
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

        if (characterTypes < MinimumCharacterTypes)
        {
            errors.Add($"Password must contain at least {MinimumCharacterTypes} of the following: uppercase letters, lowercase letters, numbers, special characters");
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

        // Require minimum score for password acceptance
        if (score < MinimumPasswordScore)
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

        // Length score
        score += Math.Min(MaxLengthScore, password.Length * PointsPerCharacter);

        // Character variety points
        if (hasUpper) score += CharacterTypePoints;
        if (hasLower) score += CharacterTypePoints;
        if (hasDigit) score += CharacterTypePoints;
        if (hasSpecial) score += CharacterTypePoints;

        // Unique characters score
        score += Math.Min(MaxUniqueCharScore, uniqueChars * PointsPerUniqueChar);

        // Entropy bonus
        var entropy = CalculateEntropy(password);
        score += Math.Min(MaxEntropyScore, (int)(entropy / EntropyDivisor));

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
            >= VeryStrongPasswordThreshold => PasswordStrength.VeryStrong,
            >= StrongPasswordThreshold => PasswordStrength.Strong,
            >= MediumPasswordThreshold => PasswordStrength.Medium,
            >= WeakPasswordThreshold => PasswordStrength.Weak,
            _ => PasswordStrength.VeryWeak
        };
    }

    public string HashPassword(string password)
    {
        // Use BCrypt with configured cost factor for optimal security/performance balance
        return BCrypt.Net.BCrypt.HashPassword(password, BCryptCostFactor);
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