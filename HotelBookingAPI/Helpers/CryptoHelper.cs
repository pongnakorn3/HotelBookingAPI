using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HotelBookingAPI.Helpers
{
    public static class CryptoHelper
    {
        // คีย์ลับขนาด 32 ตัวอักษร (ห้ามเปลี่ยนเด็ดขาดไม่งั้นข้อมูลในอนาคตจะพัง)
        private static readonly string SecretKey = "a1B2c3D4e5F6g7H8i9J0k1L2m3N4o5P6";

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            byte[] key = Encoding.UTF8.GetBytes(SecretKey);
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.GenerateIV();
                byte[] iv = aes.IV;
                using (var ms = new MemoryStream())
                {
                    ms.Write(iv, 0, iv.Length);
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs)) { sw.Write(plainText); }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);
                byte[] key = Encoding.UTF8.GetBytes(SecretKey);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    byte[] iv = new byte[aes.BlockSize / 8];
                    byte[] cipher = new byte[fullCipher.Length - iv.Length];
                    Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
                    Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);
                    aes.IV = iv;
                    using (var ms = new MemoryStream(cipher))
                    using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs)) { return sr.ReadToEnd(); }
                }
            }
            catch { return "Error: ไม่สามารถถอดรหัสได้ ข้อมูลอาจไม่ได้เข้ารหัสไว้"; }
        }
    }
}