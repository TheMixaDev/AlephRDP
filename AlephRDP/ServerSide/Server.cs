using WebSocketSharp.Server;
using WebSocketSharp;
using System;

namespace AlephRDP.ServerSide
{
    public class Server
    {
        public delegate void StatusUpdateHandler(object sender, EventArgs e);
        public event StatusUpdateHandler OnUpdateStatus;

        private static Server instance;
        private WebSocketServer wssv;
        private ServerPingTCP ping;
        private int port;
        private string password;
        private bool started = false;
        private bool notify = true;

        public Server(int port, string password)
        {
            instance = this;
            this.port = port;
            this.password = password;
        }

        public static Server Instance { get => instance; }
        public bool Started { get => started; }
        public int Port { get => port; }
        public string Password { get => password; }
        public bool Notify { get => notify; set => notify = value; }

        public void Start()
        {
            wssv = new WebSocketServer(port);
            wssv.AddWebSocketService<ServerComputer>("/Computer");
            wssv.Log.Level = LogLevel.Info;
            wssv.Log.Info("Server started");
            wssv.Start();
            ping = new ServerPingTCP(port+1);
            ping.Start();
            started = true;
            if(OnUpdateStatus != null)
                OnUpdateStatus(this, null);
        }
        public void Stop()
        {
            started = false;
            ping.Stop();
            wssv.Stop();
            if (OnUpdateStatus != null)
                OnUpdateStatus(this, null);
        }
    }
}
