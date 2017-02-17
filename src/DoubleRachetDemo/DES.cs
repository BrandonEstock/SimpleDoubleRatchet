using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoubleRachetDemo
{
    public class DES
    {
        public static byte[] GetBytesFromString(string str)
        {
            char[] arr = str.ToCharArray();
            return arr.Select(a => Convert.ToByte(a)).ToArray();
        }
        public static string GetStringFromByteArray(byte[] arr)
        {
            char[] characters = arr.Select(b => (char)b).ToArray();
            return new string(characters);
        }       
        public static string Encrypt(string text, string key)
        {
            byte[] output = null;
           
            KeyParameter keyparam = ParameterUtilities.CreateKeyParameter("DES", GetBytesFromString(key));
            IBufferedCipher cipher = CipherUtilities.GetCipher("DES/ECB/ISO7816_4PADDING");
            cipher.Init(true, keyparam);
            try
            {
                output = cipher.DoFinal(GetBytesFromString(text));
                return GetStringFromByteArray(output);
            }
            catch (System.Exception ex)
            {
                throw new CryptoException("Invalid Data");
            }
        }

        public static string EncryptB64(string text, string key)
        {
            byte[] buffer =  GetBytesFromString(Encrypt(text, key));
            return Convert.ToBase64String(buffer);
        }

        public static string Decrypt(string ciphertext, string key, string note = "")
        {
            byte[] output = null;
            
            KeyParameter keyparam = ParameterUtilities.CreateKeyParameter("DES", GetBytesFromString(key));
            IBufferedCipher cipher = CipherUtilities.GetCipher("DES/ECB/ISO7816_4PADDING");
            cipher.Init(false, keyparam);
            try
            {
                output = cipher.DoFinal(GetBytesFromString(ciphertext));
                return GetStringFromByteArray(output);
            }
            catch (System.Exception ex)
            {
                throw new CryptoException("Invalid Data");
            }
        }

        public static string DecryptB64(string text, string key)
        {
            string cipherText = GetStringFromByteArray(Convert.FromBase64String(text));
            return Decrypt(cipherText, key);
        }

    }
}
