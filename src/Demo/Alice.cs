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
        public static DRChannel Channel { get; set; }
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                CConsole.Red("!!! Invalid Arguments (RxPort, TxPort) !!!");
            }
            string PortRX = args[0];
            string PortTX = args[1];
            int rxPort;
            int txPort;
            if (Int32.TryParse(PortRX, out rxPort) == false)
            {
                CConsole.Red("!!! Invalid RX Port !!!");
            }
            if (Int32.TryParse(PortTX, out txPort) == false)
            {
                CConsole.Red("!!! Invalid TX Port !!!");
            }
            CConsole.White("~~~Started Alice~~~");
            CConsole.Gray("    RX Port: {0}", PortRX);
            CConsole.Gray("    TX Port: {0}", PortTX);
            
            try
            {
                //  !!! Plain text mode !!!
                Channel = new DRChannel(false);
                Channel.Verbose = false;

                TcpServer RxServer = new TcpServer(rxPort);
                TcpClient TxServer = new TcpClient(txPort);

                Channel.HandleTransportSend = TxServer.Write;
                RxServer.OnPacket = (string packet) =>
                {
                    Channel.HandleTransportReceive(packet);
                };
            }
            catch (Exception e)
            {
                CConsole.Red("!!! Transport Failed to Connect !!!");
                CConsole.Red("    ExceptionType: {0}\n    ExceptionMessage: {1}", e.GetType().Name, e.Message);
            }
            CConsole.White("~~~ Transport Connected ~~~\n{0}", DateTime.Now.Ticks);
            if (Channel.Open(true)) //  Alice is the initial sender
            {
                CConsole.White("~~~ Channel Open ~~~");
            }
            else
            {
                CConsole.Red("!!! Channel open timed out !!!");
                Console.ReadLine();
                return;
            }

            Channel.OnMessage = (string msg) =>
            {
                CConsole.Blue("{0}", msg);
            };
            
            bool running = true;

            while(running)
            {
                var oCol = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                string text = Console.ReadLine();
                if (text.ToUpper() == ":QUIT")
                {
                    running = false;
                }
                Console.ForegroundColor = oCol;
                Channel.Send(text);
            }          
        }
    }
}
