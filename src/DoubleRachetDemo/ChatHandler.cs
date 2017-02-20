using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DoubleRachetDemo
{
    public class ChatHandler
    {
        DRChannel Channel { get; set; }

        public ChatHandler(DRChannel channel)
        {
            Channel = channel;            
        }

        internal void StartPrompt()
        {
            bool running = true;

            while (running)
            {
                var oCol = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("> ");
                string text = Console.ReadLine();
                if (text.ToUpper() == ":QUIT")
                {
                    running = false;
                    CConsole.White("OK");
                    continue;
                }
                if (text.ToUpper() == ":SHOW_TPACKETS")
                {
                    Channel.ShowTransportPackets = true;
                    CConsole.White("OK");
                    continue;
                }
                if (text.ToUpper() == ":HIDE_TPACKETS")
                {
                    Channel.ShowTransportPackets = false;
                    CConsole.White("OK");
                    continue;
                }
                if (text.ToUpper() == ":VERBOSE_ON")
                {
                    Channel.Verbose = true;
                    CConsole.White("OK");
                    continue;
                }
                if (text.ToUpper() == ":VERBOSE_OFF")
                {
                    Channel.Verbose = false;
                    CConsole.White("OK");
                    continue;
                }
                if (text.ToUpper() == ":HELP")
                {
                    CConsole.White(":QUIT");
                    CConsole.White(":SHOW_TPACKETS");
                    CConsole.White(":HIDE_TPACKETS");
                    CConsole.White(":VERBOSE_ON");
                    CConsole.White(":VERBOSE_OFF");
                    continue;
                }
                Console.ForegroundColor = oCol;
                Channel.Send(text);
            }
        }

        bool Verbose = false;
        bool ShowNetworkPackets = false;
        string PrefilledRootKey = "asdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasdfasddffsaddf";

        public void StartSender()
        {
            Channel.Verbose = Verbose;
            Channel.ShowTransportPackets = ShowNetworkPackets;
            Channel.PrefilledRootKey = PrefilledRootKey;
            if (Channel.Open(true)) 
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
                Console.SetCursorPosition(0, Console.CursorTop);
                var d = DateTime.Now;
                CConsole.GrayInline("{0}", d.Hour.ToString() + ":" + d.Minute.ToString() + "." + d.Second.ToString() + "|" + d.Millisecond.ToString());
                CConsole.Cyan("> {0}", msg);
                var oCol = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("> ");
                Console.ForegroundColor = oCol;
            };

            Channel.Send("Message 1");
            Channel.Send("Message 1");
            Channel.WaitForResponse();
            Channel.Send("Message 3");

            StartPrompt();
        }

        public void StartReceiver()
        {
            bool first = true;
            Channel.Verbose = Verbose;
            Channel.ShowTransportPackets = ShowNetworkPackets;
            
            Channel.PrefilledRootKey = PrefilledRootKey;
            if (Channel.Open(false))  
            {
                CConsole.White("~~~ Channel Open ~~~");
            }
            else
            {
                CConsole.Red("!!! Failed to open channel !!!");
                Console.ReadLine();
                return;
            }
            Channel.OnMessage = (string msg) =>
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                var d = DateTime.Now;
                CConsole.GrayInline("{0}", d.Hour.ToString() + ":" + d.Minute.ToString() + "." + d.Second.ToString() + "|" + d.Millisecond.ToString());
                CConsole.Cyan("> {0}", msg);
                var oCol = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                if (first)
                {
                    first = false;
                }
                else
                {
                    Console.Write("> ");
                }
                Console.ForegroundColor = oCol;
            };

            CConsole.Cyan("Waiting for initial message...");
            Channel.WaitForResponse();  //  Cannot send first
            Channel.Send("Message 1");
            Channel.WaitForResponse();
            Channel.Send("Message 2");
            Channel.WaitForResponse();
            Channel.Send("Message 3");

            StartPrompt();
        }
    }
}
