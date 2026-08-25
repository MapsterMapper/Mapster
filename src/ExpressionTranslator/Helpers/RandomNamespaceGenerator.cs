using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ExpressionDebugger.Helpers
{
    public static class RandomNamespaceGenerator
    {
        public static readonly Regex CheckNameSpace = new Regex(@"^([a-zA-Z_]\w*)(\.[a-zA-Z_]\w*)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private const string Consonants = "bcdfghjklmnpqrstvwxyzBCDFGHJKLMNPQRSTVWXYZ";
        private const string Vowels = "aeiouAEIOU";
        private const string Digits = "0123456789";

        public static string Generate(string input, int minParts = 2, int maxParts = 4)
        {
            if (string.IsNullOrEmpty(input)) throw new ArgumentException("Input cannot be empty.");
            if (minParts < 1) minParts = 1;
            if (maxParts < minParts) maxParts = minParts;

            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

            long seed = BitConverter.ToInt64(hashBytes, 0);
            var random = new Random(unchecked((int)seed ^ (int)(seed >> 32)));

            int partsCount = random.Next(minParts, maxParts + 1);
            var sb = new StringBuilder();

            for (int i = 0; i < partsCount; i++)
            {
                if (i > 0) sb.Append('.');
                sb.Append(GeneratePart(random));
            }

            return sb.ToString();
        }


        public static string Generate(int minParts = 2, int maxParts = 4)
        {
            if (minParts < 1) minParts = 1;
            if (maxParts < minParts) maxParts = minParts;
            
            var _random = new Random();

            int partsCount = _random.Next(minParts, maxParts + 1);
            var sb = new StringBuilder();

            for (int i = 0; i < partsCount; i++)
            {
                if (i > 0) sb.Append('.');
                sb.Append(GeneratePart(_random));
            }

            return sb.ToString();
        }

        private static string GeneratePart(Random random, int minLength = 2, int maxLength = 10)
        {
            if (minLength < 1) minLength = 1;
            if (maxLength < minLength) maxLength = minLength;

            int length = random.Next(minLength, maxLength + 1);
            var sb = new StringBuilder(length);

            sb.Append(Consonants[random.Next(Consonants.Length)]);

            for (int i = 1; i < length; i++)
            {
                string pool = (i % 2 == 0) ? Vowels : Consonants;
                if (random.NextDouble() < 0.1) pool = Digits;
                sb.Append(pool[random.Next(pool.Length)]);
            }

            return sb.ToString();
        }
    }
}

