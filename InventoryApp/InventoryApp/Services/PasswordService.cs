using System.Security.Cryptography;
using InventoryApp.Entities;

namespace InventoryApp.Services
{
    public static class PasswordService
    {
        // Az ideiglenes jelszó ennyi ideig használható fel.
        public static readonly TimeSpan TempPasswordLifetime = TimeSpan.FromHours(72);

        public const int MinLength = 8;

        // Összetéveszthető karakterek (0/O, 1/l/I) nélkül, hogy diktálható legyen.
        private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string Lower = "abcdefghijkmnopqrstuvwxyz";
        private const string Digits = "23456789";

        public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        public static bool Verify(string password, string hash) =>
            !string.IsNullOrEmpty(hash) && BCrypt.Net.BCrypt.Verify(password, hash);

        // 12 karakteres, kriptográfiailag véletlen jelszó: van benne
        // nagybetű, kisbetű és szám is.
        public static string GenerateTemporary(int length = 12)
        {
            var all = Upper + Lower + Digits;
            var chars = new char[length];
            chars[0] = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
            chars[1] = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
            chars[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
            for (int i = 3; i < length; i++)
                chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

            // Fisher–Yates keverés, hogy a kötelező karakterek ne az elején legyenek
            for (int i = length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }

        // Új ideiglenes jelszót állít be a felhasználónak, és visszaadja sima szövegként.
        // A régi jelszó és a kiadott tokenek ezzel érvényüket vesztik.
        public static string AssignTemporary(User user, out DateTime expiresAt)
        {
            var temp = GenerateTemporary();
            expiresAt = DateTime.UtcNow.Add(TempPasswordLifetime);

            user.PasswordHash = Hash(temp);
            user.MustChangePassword = true;
            user.TempPasswordExpiresAt = expiresAt;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            return temp;
        }

        // null = megfelel, egyébként a hibaüzenet
        public static string? ValidateNew(string? password, string username)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinLength)
                return $"Az új jelszó legalább {MinLength} karakter legyen.";
            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                return "Az új jelszóban legyen betű és szám is.";
            if (string.Equals(password, username, StringComparison.OrdinalIgnoreCase))
                return "A jelszó nem egyezhet a felhasználónévvel.";
            return null;
        }
    }
}
