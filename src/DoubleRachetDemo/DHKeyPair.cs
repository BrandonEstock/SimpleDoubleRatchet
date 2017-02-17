using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities.Encoders;
using System.Diagnostics;

namespace DoubleRachetDemo
{
    public class DHKeyPair
    {
        public DHParameters Parameters { get; set; }
        private AsymmetricKeyParameter PublicKeyParameter { get; set; }
        public string PublicKey { get; set; }
        private AsymmetricKeyParameter PrivateKeyParameter { get; set; }
        public string PrivateKey { get; set; }

        public static int Bits = 512;    //  256bit=300ms, 512bit=365ms, 768bit=540ms, 1024bit=1290ms, 2048bit=_too_long_
        
        public static DHKeyPair Generate(bool debug = false)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var generator = new DHParametersGenerator();
            generator.Init(Bits, 30, new SecureRandom());
            var param = generator.GenerateParameters();

            return Generate(param, debug);
        }
        public static DHKeyPair Generate(DHParameters param, bool debug = false)
        {
            //DHParameters pa = new DHParameters();
            var generator = new DHParametersGenerator();
            generator.Init(Bits, 30, new SecureRandom());
            //DHParameters param = new DHParameters(prime, primativeRootModulo);

            var keyGen = GeneratorUtilities.GetKeyPairGenerator("DH");
            var kgp = new DHKeyGenerationParameters(new SecureRandom(), param);
            keyGen.Init(kgp);
            AsymmetricCipherKeyPair keyPair = keyGen.GenerateKeyPair();

            var dhPublicKeyParameters = keyPair.Public as DHPublicKeyParameters;
            if (dhPublicKeyParameters != null)
            {
                if (debug)
                    Console.WriteLine("Public Key: {0}", dhPublicKeyParameters.Y.ToString(16));
            }
            else
            {
                throw new Exception("Invalid Public Key");
            }

            var dhPrivateKeyParameters = keyPair.Private as DHPrivateKeyParameters;
            if (dhPrivateKeyParameters != null)
            {
                if (debug)
                    Console.WriteLine("Private Key: {0}", dhPrivateKeyParameters.X.ToString(16));
            }
            else
            {
                throw new Exception("Invalid Private Key");
            }
            return new DHKeyPair()
            {
                Parameters = param,
                PublicKeyParameter = dhPublicKeyParameters,
                PublicKey = dhPublicKeyParameters.Y.ToString(16),
                PrivateKeyParameter = dhPrivateKeyParameters,
                PrivateKey = dhPrivateKeyParameters.X.ToString(16)
            };
        }
        public string ComputeSharedSecret(string publicKey)
        {
            DHParameters param = Parameters;
            var importedKey = new DHPublicKeyParameters(new BigInteger(publicKey, 16), param);
            var internalKeyAgree = AgreementUtilities.GetBasicAgreement("DH");
            internalKeyAgree.Init(PrivateKeyParameter);
            return internalKeyAgree.CalculateAgreement(importedKey).ToString(16);
        }
    }
}
