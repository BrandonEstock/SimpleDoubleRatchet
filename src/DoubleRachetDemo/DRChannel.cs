using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DoubleRachetDemo
{
    public class DRChannel
    {
        public struct ChannelMessage
        {
            public string AcknowledgeKey { get; set; }
            public string AnnouncedKey { get; set; }
            public string CipherText { get; set; }
            public string Param_P { get; set; }
            public string Param_G { get; set; }
            public string Param_Q { get; set; }
            public int Param_L { get; set; }
            public int Id { get; set; }
            public bool PlaintextMode { get; set; }
        }
        public struct SimpleMessage
        {
            public SimpleMessage(ChannelMessage message) : this()
            {
                this.AcknowledgedId = message.AcknowledgeKey;
                this.AnnouncedId = message.AnnouncedKey;
                this.CipherText = message.CipherText;
                this.Id = message.Id;
            }

            public string AnnouncedId { get; set; }
            public string AcknowledgedId { get; set; }
            public string CipherText { get; set; }
            public int Id { get; set; }
        }
        internal DHKeyPair RachetKeyPair { get; set; }
        internal DHParameters Parameters { get; set; }
        internal string LastParterAnnouncedKey { get; set; }

        internal int RootChainCount = 0;

        internal string _RootChainKey = null;
        internal string RootChainKey {
            get
            {
                return _RootChainKey;
            }
            set
            {
                RootChainCount++;
                _RootChainKey = value;
            }
        }
        internal string SendingChainKey { get; set; }

        internal int ReceivingMessageId = 0;

        internal int SendingMessageId = 0;
        
        internal string ReceivingChainKey { get; set; }

        internal Action<string> MessageListener { get; set; }

        internal AutoResetEvent MessageEvent = new AutoResetEvent(false);

        internal bool IsOpen = false;

        public bool Verbose = true;
        public bool ShowTransportPackets = false;

        public bool PlaintextMode { get; private set; }

        internal void KDF(string input, string kdfKey, out string outputA, out string outputB)
        {
            string cipherText = DES.Encrypt(input, kdfKey);
            outputA = cipherText;
            outputB = new String(cipherText.Reverse().ToArray());
        }

        public DRChannel(bool plaintextMode = false)
        {
            PlaintextMode = plaintextMode;
        }               
      
        public bool Open(bool sender)
        {
            if ( !sender )
            {
                //  BOB
                MessageEvent.WaitOne(30 * 1000); //  Wait until fully opened or timeout in 30s
                if ( IsOpen == false )
                {
                    return false;
                }

                //  Send acknowledge
                if ( Verbose ) 
                    CConsole.DarkCyan("Acknowledging = {0}", LastParterAnnouncedKey);
                if (Verbose)
                    CConsole.DarkMagenta("Announcing = {0}", RachetKeyPair.PublicKey);

                RootChainKey = RachetKeyPair.ComputeSharedSecret(LastParterAnnouncedKey);
                string outputA;
                string outputB;
                KDF(RootChainKey, RootChainKey, out outputA, out outputB);
                RootChainKey = outputA;
                SendingChainKey = outputB;
                if (Verbose)
                    CConsole.DarkYellow("RootChainKey = {0}", H(RootChainKey));
                if (Verbose) 
                    CConsole.Magenta("SendingChainKey = {0}", H(SendingChainKey));

                ChannelMessage msg = new ChannelMessage()
                {
                    AcknowledgeKey = LastParterAnnouncedKey,
                    AnnouncedKey = RachetKeyPair.PublicKey,
                    CipherText = null
                };
                if (!SerializeAndSend(msg))
                {
                    CConsole.Red("!!! Error sending initial announce !!!");
                }
            }
            else
            {
                //  ALICE
                //  Send announce
                RachetKeyPair = DHKeyPair.Generate();
                Parameters = RachetKeyPair.Parameters;
                ChannelMessage msg = new ChannelMessage()
                {
                    AcknowledgeKey = null,
                    AnnouncedKey = RachetKeyPair.PublicKey,
                    Param_P = RachetKeyPair.Parameters.P.ToString(16),
                    Param_G = RachetKeyPair.Parameters.G.ToString(16),
                    Param_Q = RachetKeyPair.Parameters.Q.ToString(16),
                    Param_L = RachetKeyPair.Parameters.L,
                    CipherText = null,
                    PlaintextMode = PlaintextMode
                };
                if (Verbose)
                    CConsole.DarkCyan("Announcing = {0}", RachetKeyPair.PublicKey);
                if (!SerializeAndSend(msg))
                {
                    CConsole.Red("!!! Error sending initial announce !!!");
                }
                MessageEvent.WaitOne(30 * 1000); //  wait till fully opened or timeout in 30s
                if ( IsOpen == false )
                {
                    return false;
                }
            }
            return true;
        }

        public Action<string> HandleTransportSend { get; set; }

        public void HandleMessageReceive(ChannelMessage message)
        {
            SimpleMessage simpleMessage = new SimpleMessage(message);
            if (ShowTransportPackets )
            {
                CConsole.Gray("{0}", JsonConvert.SerializeObject(simpleMessage, Formatting.Indented));
            }
            if (message.CipherText == null)
            {
                //  It's not a message packet
                if (message.AnnouncedKey != null && message.AcknowledgeKey == null)
                {
                    //  BOB
                    //  Initial announce from other side
                    if (PlaintextMode != message.PlaintextMode)
                    {
                        CConsole.Red("!!! Invalid Far-side configuration! Near-Side PlaintextMode = {0}, Far-Side PlaintextMode = {1}", PlaintextMode, message.PlaintextMode);
                        IsOpen = false;
                        MessageEvent.Set();
                        return;
                        //throw new Exception("PlaintextMode Mismatch");
                    }
                    Parameters = new DHParameters(
                        new BigInteger(message.Param_P, 16),
                        new BigInteger(message.Param_G, 16),
                        new BigInteger(message.Param_Q, 16),
                        message.Param_L);
                    RachetKeyPair = DHKeyPair.Generate(Parameters);
                    if (Verbose)
                        CConsole.Yellow("RootChainKey = {0}", H(RachetKeyPair.ComputeSharedSecret(message.AnnouncedKey)));
                    LastParterAnnouncedKey = message.AnnouncedKey;

                    IsOpen = true;
                    MessageEvent.Set();
                }
                else if (message.AnnouncedKey != null && message.AcknowledgeKey != null)
                {
                    //  ALICE
                    //  Initial acknowledge from other side
                    if (Verbose)
                        CConsole.DarkMagenta("Acknowledging = {0}", message.AnnouncedKey);
                    LastParterAnnouncedKey = message.AnnouncedKey;
                    if (message.AcknowledgeKey != RachetKeyPair.PublicKey)
                    {
                        CConsole.Red("!!! Invalid AcknowledgedKey !!!");
                        throw new Exception("Invalid AcknowledgedKey");
                    }
                    else
                    {                        
                        RootChainKey = RachetKeyPair.ComputeSharedSecret(message.AnnouncedKey);   //FIRST RootChainKey   
                        if (Verbose)
                            CConsole.Yellow("RootChainKey = {0}", H(RootChainKey));
                        string outputA1;
                        string outputB1;
                        KDF(RootChainKey, RootChainKey, out outputA1, out outputB1);
                        RootChainKey = outputA1;
                        ReceivingChainKey = outputB1;
                        if (Verbose)
                            CConsole.DarkYellow("RootChainKey = {0}", H(RootChainKey));
                        if (Verbose)
                            CConsole.Magenta("ReceivingChainKey = {0}", H(ReceivingChainKey));
                        string outputA2;
                        string outputB2;
                        RachetKeyPair = DHKeyPair.Generate(Parameters);
                        if (Verbose)
                        {
                            if (RootChainCount % 2 == 1)
                            {
                                CConsole.DarkMagenta("Announcing = {0}", H(RachetKeyPair.PublicKey));
                            }
                            else
                            {
                                CConsole.DarkCyan("Announcing = {0}", H(RachetKeyPair.PublicKey));
                            }
                        }
                        string secret = RachetKeyPair.ComputeSharedSecret(message.AnnouncedKey);
                        KDF(secret, RootChainKey, out outputA2, out outputB2);
                        RootChainKey = outputA2;
                        SendingChainKey = outputB2;
                        if (Verbose)
                            CConsole.Yellow("RootChainKey = {0}", H(RootChainKey));
                    }
                    IsOpen = true;
                    MessageEvent.Set();
                }
            }
            else
            {
                //  It's a message packet! Do the decrypt
                string text;
                if (!Decrypt(message, out text))
                {
                    throw new Exception("Failed to Decrypt packet!");
                }
                OnMessage(text);
                MessageListener?.Invoke(text);
            }
        }

        public void HandleTransportReceive(string tPacket)
        {
            ChannelMessage? message;
            List<ChannelMessage> messages;
            if ( !Deserialize(tPacket, out message, out messages) )
            {
                throw new Exception("Failed to Deserialize packet!");
            }
            if ( messages != null )
            {
                //  TODO out of order messages
                //  Sort the message and handle individually
                foreach(ChannelMessage msg in messages)
                {
                    HandleMessageReceive(msg);
                }
                return;
            }
            if ( message.HasValue )
            {
                HandleMessageReceive(message.Value);
            }
            
        }

        public string H(string text)
        {
            return DES.GetStringFromByteArray(Hex.Encode(DES.GetBytesFromString(text)));
        }

        public Action<string> OnMessage { get; set; }

        public void Send(string message)
        {
            ChannelMessage msg;
            if (!Encrypt(message, out msg))
            {
                throw new Exception("Failed to encrypt message!");
            }
            if (!SerializeAndSend(msg) )
            {
                throw new Exception("Failed to serialize or send message!");
            }
        }

        public bool DHRachetNeeded(string announcedKey, string acknowledgedKey)
        {
            if ( announcedKey != LastParterAnnouncedKey )
            {
                //  Ratchet Step needed, new key
                if ( Verbose)
                {
                    if (RootChainCount % 2 == 1)
                    {
                        CConsole.DarkMagenta("Acknowledging = {0}", H(announcedKey));
                    }
                    else
                    {
                        CConsole.DarkCyan("Acknowledging = {0}", H(announcedKey));
                    }
                }
                LastParterAnnouncedKey = announcedKey;
                return true;
            }
            else
            {
                //  Ratchet step seen already
                return false;
            }
        }
       
        internal bool Encrypt(string text, out ChannelMessage message)
        {
            if (Verbose)
            {
                if (RootChainCount % 2 == 1)
                {
                    CConsole.Blue("SendingChainKey = {0}", H(SendingChainKey));
                }
                else
                {
                    CConsole.Magenta("SendingChainKey = {0}", H(SendingChainKey));
                }
            }
            if ( PlaintextMode )
            {
                message = new ChannelMessage()
                {
                    Id = SendingMessageId++,
                    CipherText = text,
                    AnnouncedKey = RachetKeyPair.PublicKey,
                    AcknowledgeKey = LastParterAnnouncedKey,
                    PlaintextMode = PlaintextMode
                };
                return true;
            }

            string messageKey;
            string outputA;
            string outputB;
            KDF(SendingChainKey, SendingChainKey, out outputA, out outputB);

            messageKey = outputA;
            SendingChainKey = outputB;

            if (Verbose)
            {
                if (RootChainCount % 2 == 1)
                {
                    CConsole.Magenta("SendingChainKey = {0}", H(SendingChainKey));
                }
                else
                {
                    CConsole.Blue("SendingChainKey = {0}", H(SendingChainKey));
                }
            }

            if (Verbose)
                CConsole.DarkGreen("MessageKey = {0}", H(messageKey));

            message = new ChannelMessage()
            {
                Id = SendingMessageId++,
                CipherText = DES.EncryptB64(text, messageKey),
                AnnouncedKey = RachetKeyPair.PublicKey,
                AcknowledgeKey = LastParterAnnouncedKey
            };
            return true;
        }

        internal bool Decrypt(ChannelMessage message, out string text)
        {
            if ( PlaintextMode && message.PlaintextMode )
            {
                text = message.CipherText;
                return true;
            }
            if ( DHRachetNeeded(message.AnnouncedKey, message.AcknowledgeKey ) )
            {
                string secretA = RachetKeyPair.ComputeSharedSecret(message.AnnouncedKey);
                string outputA1;
                string outputB1;
                KDF(secretA, RootChainKey, out outputA1, out outputB1);
                RootChainKey = outputA1;
                ReceivingChainKey = outputB1;
                if (Verbose)
                {
                    if( RootChainCount % 2 == 1)
                    {
                        CConsole.Yellow("RootChainKey = {0}", H(RootChainKey));
                    }
                    else
                    {
                        CConsole.DarkYellow("RootChainKey = {0}", H(RootChainKey));
                    }
                }
                if (Verbose)
                {
                    if (RootChainCount % 2 == 1)
                    {
                        CConsole.Blue("ReceivingChainKey = {0}", H(ReceivingChainKey));
                    }
                    else
                    {
                        CConsole.Magenta("ReceivingChainKey = {0}", H(ReceivingChainKey));
                    }
                }

                string messageKey;
                string outputA;
                string outputB;
                KDF(ReceivingChainKey, ReceivingChainKey, out outputA, out outputB);

                messageKey = outputA;
                ReceivingChainKey = outputB;

                if (Verbose)
                {
                    if (RootChainCount % 2 == 1)
                    {
                        CConsole.Magenta("ReceivingChainKey = {0}", H(ReceivingChainKey));
                    }
                    else
                    {
                        CConsole.Blue("ReceivingChainKey = {0}", H(ReceivingChainKey));
                    }
                }
                

                if (Verbose)
                    CConsole.DarkGreen("MessageKey = {0}", H(messageKey));

                text = DES.DecryptB64(message.CipherText, messageKey);

                RachetKeyPair = DHKeyPair.Generate(RachetKeyPair.Parameters);
                if ( Verbose)
                {
                    if (RootChainCount % 2 == 1)
                    {
                        CConsole.DarkMagenta("Announcing = {0}", H(RachetKeyPair.PublicKey));
                    }
                    else
                    {
                        CConsole.DarkCyan("Announcing = {0}", H(RachetKeyPair.PublicKey));
                    }
                }
                string secretB = RachetKeyPair.ComputeSharedSecret(message.AnnouncedKey);
                string outputA2;
                string outputB2;
                KDF(secretB, RootChainKey, out outputA2, out outputB2);
                RootChainKey = outputA2;
                SendingChainKey = outputB2;
                if (Verbose)
                {
                    if (RootChainCount % 2 == 1 )
                    {
                        CConsole.Yellow("RootChainKey = {0}", H(RootChainKey));
                    }
                    else
                    {
                        CConsole.DarkYellow("RootChainKey = {0}", H(RootChainKey));
                    }
                }

                ReceivingMessageId++;
                return true;
            }
            else
            {                
                if (Verbose)
                    CConsole.Red("ReceivingChainKey = {0}", H(ReceivingChainKey));

                string messageKey = ReceivingChainKey;

                if (Verbose)
                    CConsole.DarkGreen("MessageKey = {0}", H(messageKey));

                text = DES.DecryptB64(message.CipherText, messageKey);

                ReceivingMessageId++;
                return true;
            }
        }

        internal bool SerializeAndSend(ChannelMessage message)
        {           
            string packet = JsonConvert.SerializeObject(message, Formatting.Indented);
            HandleTransportSend(packet);
            return true;            
        }

        internal bool Deserialize(string packet, out ChannelMessage? message, out List<ChannelMessage> messages)
        {
            try
            {
                message = JsonConvert.DeserializeObject<ChannelMessage>(packet);
                messages = null;
                return true;
            }
            catch(JsonReaderException ex)
            {
                packet = "[" + packet.Replace("}{", "},{") + "]";
                try
                {
                    messages = JsonConvert.DeserializeObject<List<ChannelMessage>>(packet);
                    message = null;
                    return true;
                }
                catch (Exception e)
                {
                    message = null;
                    messages = null;
                    return false;
                }
            }
        } 
        
        public string WaitForResponse()
        {
            string msg = null;
            MessageListener = (s) =>
            {
                msg = s;
                MessageEvent.Set();
            };
            MessageEvent.WaitOne();
            return msg;
        }  
      }
}
