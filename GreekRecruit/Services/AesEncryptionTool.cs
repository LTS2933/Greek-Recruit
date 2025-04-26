using System.Security.Cryptography;

namespace GreekRecruit.Services;    

public static class AesEncryptionHelper
{
    private static readonly string EncryptionKey = Environment.GetEnvironmentVariable("AES_KEY"); // Store this securely!

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return null;

        using (var aes = Aes.Create())
        {
            var key = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x43, 0x87, 0x23, 0x72, 0x20, 0x20, 0x65, 0x76 });
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return null;

        using (var aes = Aes.Create())
        {
            var key = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x43, 0x87, 0x23, 0x72, 0x20, 0x20, 0x65, 0x76 });
            aes.Key = key.GetBytes(32);
            aes.IV = key.GetBytes(16);

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
