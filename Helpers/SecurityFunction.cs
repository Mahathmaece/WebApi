
using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace WebApi.Helpers
{
    public class SecurityFunction
    {
        private static string iv1 = "AAECAwQFBgcICQoL";
        private static string iv2 = "BCZ3X0En5CAwQQPO";

        public static string DecryptAES1(string hashedInput, string hashKey)
        {
            return DecryptAES(hashedInput, hashKey, iv1);
        }

        public static string EncryptAES1(string plainText, string hashKey)
        {
            return EncryptAES(plainText, hashKey, iv1);
        }

        public static string DecryptAES2(string hashedInput, string hashKey)
        {
            return DecryptAES(hashedInput, hashKey, iv2);
        }

        public static string EncryptAES2(string plainText, string hashKey)
        {
            return EncryptAES(plainText, hashKey, iv2);
        }


        public static string DecryptAES(string hashedInput, string hashKey, string iv)
        {
            string plainText = "";
            try
            {
                byte[] encryptedText = Convert.FromBase64String(hashedInput);
                System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();
                AesManaged tdes = new AesManaged();
                tdes.Key = UTF8.GetBytes(hashKey);
                tdes.IV = UTF8.GetBytes(iv);
                tdes.Mode = CipherMode.CBC;
                tdes.Padding = PaddingMode.PKCS7;
                ICryptoTransform crypt = tdes.CreateDecryptor();
                byte[] cipher = crypt.TransformFinalBlock(encryptedText, 0, encryptedText.Length);
                plainText = UTF8.GetString(cipher);

            }
            catch (Exception ex)
            {

            }
            return plainText;
        }

        public static string EncryptAES(string plainText, string hashKey, string iv)
        {
            string hashedText = "";
            try
            {
                System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();
                AesManaged tdes = new AesManaged();
                tdes.Key = UTF8.GetBytes(hashKey);
                tdes.IV = UTF8.GetBytes(iv);
                tdes.Mode = CipherMode.CBC;
                tdes.Padding = PaddingMode.PKCS7;
                ICryptoTransform encryptor = tdes.CreateEncryptor();

                byte[] plainTextBytes = UTF8.GetBytes(plainText);
                byte[] cipher = encryptor.TransformFinalBlock(plainTextBytes, 0, plainTextBytes.Length);
                hashedText = Convert.ToBase64String(cipher);
            }
            catch (Exception ex)
            {

            }
            return hashedText;
        }

        public static bool IsValidPassword(string plainText)
        {
            Regex regex = new Regex(@"^((?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?=.*[-!$%^&()_+|~=`{}:<>?,.@#]).{8,16})$");
            /*
            (           # Start of group
              (?=.*\d)      #   must contains one digit from 0-9
              (?=.*[a-z])       #   must contains one lowercase characters
              (?=.*[A-Z])       #   must contains one uppercase characters
              (?=.*[-!$%^&()_+|~=`{}:<>?,.@#])      #   must contains one special symbols in the list "-!$%^&()_+|~=`{}:<>?,.@#"
                          .     #     match anything with previous condition checking
                            {8,16}  #        length at least 8 characters and maximum of 16 
            )       
            */
            Match match = regex.Match(plainText);
            return match.Success;
        }
    }
}
