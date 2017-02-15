using DoubleRachetDemo;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace Demo
{
    public class Program
    {
        public static Encoding CurrentEncoding = Encoding.GetEncoding(437); // Encoding.Unicode;

        public static string GetStringFromParameters(DHParameters param)
        {
            return String.Format("{{\n        P={0},\n        G={1},\n        Q={2},\n        L={3}\n    }}", param.P, param.G, param.Q, param.L);
        }

        public static void Alice(string message, params object[] formatting)
        {
            ConsoleColor col = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("* " + message, formatting);
            Console.ForegroundColor = col;
        }
        public static void AliceSent(string message, params object[] formatting)
        {
            ConsoleColor col = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("* " + message, formatting);
            Console.ForegroundColor = col;
        }
        public static void Bob(string message, params object[] formatting)
        {
            ConsoleColor col = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("* " + message, formatting);
            Console.ForegroundColor = col;
        }
        public static void BobSent(string message, params object[] formatting)
        {
            ConsoleColor col = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("* " + message, formatting);
            Console.ForegroundColor = col;
        }
        public static void Note(string message, params object[] formatting)
        {
            ConsoleColor col = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n" + new string('-', message.Length) + "\n" + message + "\n" + new string('-', message.Length) + "\n", formatting);
            Console.ForegroundColor = col;
        }
        public static void Network()
        {
            ConsoleColor col = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("           ---------------------->");
            Console.WriteLine("                Network Request");
            Console.WriteLine("          <----------------------");
            Console.ForegroundColor = col;
        }

        public static void WorkingDHRachetDemo()
        {
            DHKeyPair.Bits = 256;

            Note("Step 1: Exchange Public Keys");
            Alice("START Alice");
            Alice("Generate a KeyPair called A.KP1 using Parameters");
            var A_KP1 = DHKeyPair.Generate();
            AliceSent("Sent {{\n    Parameters={0},\n    A.KP1.Public={1}\n}} to Bob", GetStringFromParameters(A_KP1.Parameters), A_KP1.PublicKey);

            Network();
            Bob("START Bob");
            Bob("Generate a KeyPair called B.KP1 using Parameters");
            var B_KP1 = DHKeyPair.Generate(A_KP1.Parameters);
            BobSent("Sent {{\n    B.KP1.Public={0}\n}} to Alice", B_KP1.PublicKey);

            Network();

            //------------------------------------------------------------
            Note("Step 2: Compute Secret1");
            string SecretA1 = A_KP1.ComputeSharedSecret(B_KP1.PublicKey);
            Alice("Secret1 is computed from    A.KP1.Private    and    B.KP1.Public");
            Alice("Computed Secret1 to be {0}", SecretA1);
            Bob("Secret1 is computed from    B.KP1.Private    and    A.KP1.Public");
            string SecretB1 = B_KP1.ComputeSharedSecret(A_KP1.PublicKey);
            Bob("Computed Secret1 to be {0}", SecretB1);
            //------------------------------------------------------------
            Note("Step 3: Alice sends message to Bob using Secret1");
            string Message1 = "Hello Bob! I'm Alice.";
            string CipherText1 = DES.Encrypt(Message1, SecretA1);
            Alice("Encrypted '{0}' to CipherText1 using Secret1", Message1);

            var A_KP2 = DHKeyPair.Generate(A_KP1.Parameters);
            Alice("Generated a KeyPair called A.KP2 using Parameters");

            Alice("Secret2 is computed from     A.KP2.Private and B.KP1.Public");
            string SecretA2 = B_KP1.ComputeSharedSecret(A_KP2.PublicKey);
            Alice("Computed Secret2 to be {0}", SecretA2);
            AliceSent("Sent {{\n    CipherText1='{0}'\n    A.KP2.Public={1}\n}} to Bob", CipherText1, A_KP2.PublicKey);

            Network();

            string Message1_ = DES.Decrypt(CipherText1, SecretB1);
            Bob("Decrypted CipherText1 to '{0}' using Secret1", Message1_);

            var B_KP2 = DHKeyPair.Generate(A_KP1.Parameters);
            Bob("Generated a KeyPair called B.KP2 using Parameters");

            Bob("Secret2 is computed from     B.KP1.Private and A.KP2.Public");
            string SecretB2 = B_KP1.ComputeSharedSecret(A_KP2.PublicKey);
            Bob("Computed Secret2 to be {0}", SecretB2);

            //------------------------------------------------------------
            Note("Step 4: Bob sends a message to Alice using Secret2");
            string Message2 = "Hello Alice! I'm Bob as you already know.";
            string CipherText2 = DES.Encrypt(Message2, SecretB2);
            Bob("Encrypted '{0}' to CipherText2 using Secret2", Message2);

            var B_KP3 = DHKeyPair.Generate(A_KP1.Parameters);
            Bob("Generated a KeyPair B.KP3 ");
            //  [Bob]       Send Alice {$cipher, B2.Public}
            //  ---------->
            //  <----------           

        }

        public static void WorkingChainKeyDemo()
        {
            DoubleRachetChain alice = new DoubleRachetChain();
            DoubleRachetChain bob = new DoubleRachetChain();

            Note("Initializing...");

            string alicePublicKey1;
            string bobPublicKey1;
            DHParameters Parameters = alice.BeginInit(out alicePublicKey1);
            bob.BeginInit(out bobPublicKey1, Parameters);
            alice.EndInit(bobPublicKey1);
            bob.EndInit(alicePublicKey1);

            Note("Initialized!");

            string alicePublicKey2;
            string cipherText1 = alice.Encrypt("Hello World", out alicePublicKey2);
            string cipherText2 = alice.Encrypt("HELLO WORLD", out alicePublicKey2);

            Note("Alice encrypted CipherText1 '{0}'", cipherText1);
            Note("Alice encrypted CipherText2 '{0}'", cipherText2);

            string message1 = bob.Decrypt(cipherText1, alicePublicKey2);
            string message2 = bob.Decrypt(cipherText2, alicePublicKey2);

            Note("Bob decrypted CipherText1 into '{0}'", message1);
            Note("Bob decrypted CipherText2 into '{0}'", message2);

            string bobPublicKey2;
            string cipherText3 = bob.Encrypt("Hello Alice. Stop bugging me please", out bobPublicKey2);

            Note("Alice encrypted CipherText3 '{0}'", cipherText3);

            string message3 = alice.Decrypt(cipherText3, bobPublicKey2);

            Note("Alice decrypted CipherText3 into '{0}'", message3);

        }

        public static void Main(string[] args)
        {

            //WorkingDHRachetDemo();

            WorkingChainKeyDemo();

            Console.ReadLine();
        }
    }
}
