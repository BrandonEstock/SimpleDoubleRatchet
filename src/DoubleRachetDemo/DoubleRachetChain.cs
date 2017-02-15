using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoubleRachetDemo
{
    public class DoubleRachetChain
    {
        public DHKeyPair KeyPair { get; private set; }
        public string KnownPartnerKey { get; private set; }
        private string RootKey { get; set; }
        private string SendingKey { get; set; }
        private string ReceivingKey { get; set; }
        private bool FirstSender { get; set; }

        private void Print(string text, string formatting, params object[] arr)
        {
            ConsoleColor org = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(text);
            Console.ForegroundColor = org;
            Console.WriteLine(formatting, arr);
        }

        public void EndInit(string announcedKey)
        {
            RootKey = KeyPair.ComputeSharedSecret(announcedKey);
            KnownPartnerKey = announcedKey;
            if ( FirstSender )
            {
                SendingKey = KeyPair.ComputeSharedSecret(announcedKey);
                Print("SendingKey = ", "{0}", SendingKey);
                RootKey = SendingKey = DES.Encrypt(SendingKey, RootKey);
                Print("SendingKey = ", "{0}", SendingKey);
            }
            else
            {
                ReceivingKey = KeyPair.ComputeSharedSecret(announcedKey);
                Print("ReceivingKey = ", "{0}", ReceivingKey);
                RootKey = ReceivingKey = DES.Encrypt(ReceivingKey, RootKey);
                Print("ReceivingKey = ", "{0}", ReceivingKey);
            }

            Print("RootKey = ", "{0}", RootKey);
        }        
        public DHParameters BeginInit(out string announcedKey, DHParameters param = null)
        {
            if ( param == null )
            {
                FirstSender = true;
                KeyPair = DHKeyPair.Generate();
            }
            else
            {
                FirstSender = false;
                KeyPair = DHKeyPair.Generate(param);
            }

            announcedKey = KeyPair.PublicKey;

            return KeyPair.Parameters;
        }
        public string Encrypt(string message, out string announcedKey)
        {                
            if ( SendingKey == null )
            {
                throw new Exception("Not initialized!");
            }

            RootKey = DES.Encrypt(SendingKey, RootKey);

            announcedKey = KeyPair.PublicKey;

            return DES.Encrypt(message, RootKey);
        }

        public string Decrypt(string ciphertext, string announcedKey)
        {
            if ( ReceivingKey == null )
            {
                ReceivingKey = KeyPair.ComputeSharedSecret(announcedKey);
            }
            if ( KnownPartnerKey == announcedKey )
            {
                if ( SendingKey == null )
                {
                    SendingKey = KeyPair.ComputeSharedSecret(announcedKey);
                }
                Print("Existing; First Sender = ", "{0}", FirstSender);

                RootKey = DES.Encrypt(ReceivingKey, RootKey);
                return DES.Decrypt(ciphertext, RootKey);
            }
            Print("Generated; First Sender = ", "{0}", FirstSender);
            KnownPartnerKey = announcedKey;

            ReceivingKey = KeyPair.ComputeSharedSecret(announcedKey);

            KeyPair = DHKeyPair.Generate(KeyPair.Parameters);

            SendingKey = KeyPair.ComputeSharedSecret(announcedKey);

            return null;
        }
    }
}
