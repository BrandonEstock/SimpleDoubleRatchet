using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoubleRachetDemo
{
    public class TcpClient
    {
        public int Port { get; private set; }
        public System.Net.Sockets.TcpClient Client { get; private set; } 
        public System.Net.Sockets.NetworkStream Stream { get; private set; }
        public TcpClient(string ipAddr, int port)
        {
            Client = new System.Net.Sockets.TcpClient();
            CConsole.DarkGray("[TcpClient] Connecting");
            Client.ConnectAsync(ipAddr, port).Wait();
            if ( !Client.Connected )
            {
                CConsole.Red("!!! Error Connecting TcpClient to port {0} !!!", port);
                throw new Exception("notConnected");
            }
            else
            {
                CConsole.DarkGray("[TcpClient] Connected");
                Stream = Client.GetStream();
            }
        }

        public void Write(string obj)
        {
            byte[] arr = Encoding.ASCII.GetBytes(obj);
            Stream.Write(arr, 0, arr.Length);
            Stream.Flush();
        }
    }
}
