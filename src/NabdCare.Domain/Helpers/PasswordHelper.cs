using System.Security.Cryptography;
using System.Text;

namespace NabdCare.Domain.Helpers;

public static class PasswordHelper
{
    // Recommended settings for PBKDF2
    private const int SaltSize = 16; // 128 bit
    private const int HashSize = 32; // 256 bit
    private const int Iterations = 100_000;

    // Returns {iterations}.{base64Salt}.{base64Hash}
    public static string HashPassword(string password)
    {
        // Generate salt
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Hash password with salt
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(HashSize);

        // Format: iterations.salt.hash (all base64)
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var parts = storedHash.Split('.', 3);
            if (parts.Length != 3)
                return false;

            int iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] hash = Convert.FromBase64String(parts[2]);

            // Hash incoming password
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            byte[] attemptedHash = pbkdf2.GetBytes(hash.Length);

            // Compare hashes securely
            return CryptographicOperations.FixedTimeEquals(hash, attemptedHash);
        }
        catch
        {
            return false;
        }
    }
}