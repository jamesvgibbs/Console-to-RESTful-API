using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RalExtractorByDateRange
{
    public static class Cipher
    {
        // This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
        // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
        // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
        private const string InitVector = "aIU%g0vuCyca4VUR";

        // This constant is used to determine the keysize of the encryption algorithm.
        private const int Keysize = 256;

        // PasswordDerivedBytes is deplicated all new api keys will use the new Rfc2898DerivedBytes
        internal static string Encrypt(string plainText, string passPhrase)
        {
            var symmetricKey = new RijndaelManaged();
            byte[] initVectorBytes = Encoding.UTF8.GetBytes(InitVector);
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherTextBytes;

            var password = new Rfc2898DeriveBytes(passPhrase, initVectorBytes);
            byte[] keyBytes = password.GetBytes(Keysize / 8);

            symmetricKey.Mode = CipherMode.CBC;

            var encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);

            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                    cryptoStream.FlushFinalBlock();
                    cipherTextBytes = memoryStream.ToArray();
                    memoryStream.Close();
                    cryptoStream.Close();
                }
            }

            return Convert.ToBase64String(cipherTextBytes);
        }

        internal static string Decrypt(string cipherText, string passPhrase)
        {
            // try new, if fails, try old, if fails return null
            string decryptedString;
            try
            {
                decryptedString = NewPasswordDecrypt(cipherText, passPhrase);
                if (string.IsNullOrEmpty(decryptedString))
                {
                    decryptedString = OldPasswordDecrypt(cipherText, passPhrase);
                }
            }
            catch (Exception)
            {
                return null;
            }

            return decryptedString;
        }

        private static string OldPasswordDecrypt(string cipherText, string passPhrase)
        {
            try
            {
                var symmetricKey = new RijndaelManaged();
                byte[] plainTextBytes;
                byte[] initVectorBytes = Encoding.ASCII.GetBytes(InitVector);
                byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
                int decryptedByteCount;

                var password = new PasswordDeriveBytes(passPhrase, null);
                byte[] keyBytes = password.GetBytes(Keysize / 8);
                symmetricKey.Mode = CipherMode.CBC;

                ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
                using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        plainTextBytes = new byte[cipherTextBytes.Length];
                        decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                        memoryStream.Close();
                        cryptoStream.Close();
                    }
                }

                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string NewPasswordDecrypt(string cipherText, string passPhrase)
        {
            try
            {
                var symmetricKey = new RijndaelManaged();
                byte[] plainTextBytes;
                byte[] initVectorBytes = Encoding.ASCII.GetBytes(InitVector);
                byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
                int decryptedByteCount;

                var password = new Rfc2898DeriveBytes(passPhrase, initVectorBytes);
                byte[] keyBytes = password.GetBytes(Keysize / 8);
                symmetricKey.Mode = CipherMode.CBC;
                ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
                using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        plainTextBytes = new byte[cipherTextBytes.Length];
                        decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                        memoryStream.Close();
                        cryptoStream.Close();
                    }
                }

                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
