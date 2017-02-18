using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DoubleRachetDemo
{
    public class TcpServer
    {
        public Action<string> OnPacket { get; set; }
        public TcpListener Listener { get; set; }
        private void HandleConnection(System.Net.Sockets.TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            while (client.Connected)
            {
                if (stream.DataAvailable == false)
                {
                    Thread.Sleep(100);
                }
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
                string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                OnPacket?.Invoke(dataReceived);
                Thread.Sleep(100);
            }        
        }

        public TcpServer(string ipAddr, int rxPort)
        {
            Listener = new TcpListener(IPAddress.Parse(ipAddr), rxPort);
            Listener.Start();

            new Thread(new ThreadStart(() =>
            {
                var c = Listener.AcceptTcpClientAsync(); c.Wait();
                HandleConnection(c.Result);
            })).Start();           
        }
    }
}
