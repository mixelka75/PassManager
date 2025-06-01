using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Models
{
    public class EncryptionService
    {
        private const int Iterations = 10000;
        private const int KeySize = 256;
        
        public static byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
        
        public static byte[] DeriveKeyFromPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(KeySize / 8);
            }
        }
        
        public static string Encrypt(string plainText, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        
        public static string Decrypt(string cipherText, byte[] key)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                byte[] iv = new byte[aes.BlockSize / 8];
                Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;
                
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, iv.Length, cipherBytes.Length - iv.Length);
                        cs.FlushFinalBlock();
                    }
                    
                    return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
        }
        
        public static string GeneratePassword(int length, bool useUppercase, bool useLowercase, bool useNumbers, bool useSpecial)
        {
            const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
            const string numberChars = "0123456789";
            const string specialChars = "!@#$%^&*()_-+=<>?";
            
            StringBuilder charSet = new StringBuilder();
            if (useUppercase) charSet.Append(uppercaseChars);
            if (useLowercase) charSet.Append(lowercaseChars);
            if (useNumbers) charSet.Append(numberChars);
            if (useSpecial) charSet.Append(specialChars);
            
            if (charSet.Length == 0)
            {
                charSet.Append(lowercaseChars);
                charSet.Append(uppercaseChars);
                charSet.Append(numberChars);
            }
            
            char[] chars = new char[length];
            byte[] randomData = new byte[length];
            
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomData);
            }
            
            for (int i = 0; i < length; i++)
            {
                chars[i] = charSet[randomData[i] % charSet.Length];
            }
            
            return new string(chars);
        }
    }
}