using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AlephRDP.ServerSide
{
    internal class ServerPingTCP
    {
        HttpServer httpServer;
        public ServerPingTCP(int port)
        {
            httpServer = new HttpServer(port);
        }
        public void Start()
        {
            httpServer.Start();
        }
        public void Stop()
        {
            httpServer?.Stop();
        }
        public interface IHttpServer
        {
            void Start();
            void Stop();
        }

        public class HttpServer : IHttpServer
        {
            private readonly TcpListener listener;

            public HttpServer(int port)
            {
                this.listener = new TcpListener(IPAddress.Any, port);
            }

            public void Start()
            {
                new Thread(() => {
                    this.listener.Start();
                    while (true)
                    {
                        try
                        {
                            var client = this.listener.AcceptTcpClient();
                            var buffer = new byte[10240];
                            var stream = client.GetStream();
                            var length = stream.Read(buffer, 0, buffer.Length);
                            var incomingMessage = Encoding.UTF8.GetString(buffer, 0, length);
                            var result = "PING";
                            var response = Encoding.UTF8.GetBytes(
                                    "HTTP/1.0 200 OK" + Environment.NewLine
                                    + "Content-Length: " + result.Length + Environment.NewLine
                                    + "Content-Type: " + "text/plain" + Environment.NewLine
                                    + Environment.NewLine
                                    + result
                                    + Environment.NewLine + Environment.NewLine);
                            stream.Write(response, 0, response.Length);
                            //Console.WriteLine("Incoming message: {0}", incomingMessage);
                        }
                        catch { break; }
                    }
                }).Start();
            }
            public void Stop()
            {
                this.listener?.Stop();
            }
        }
    }
}
