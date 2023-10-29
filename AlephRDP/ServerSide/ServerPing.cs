using System.Net;
using System.Threading.Tasks;

namespace AlephRDP.ServerSide
{
    // Deprecated
    internal class ServerPing
    {
        private HttpListener listener;
        private Task task;
        public ServerPing(int port)
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:" + port.ToString()+"/");
        }
        public void Start()
        {
            listener.Start();
            task = Task.Run(() => HandleRequests());
        }
        public void Stop()
        {
            listener.Stop();
            listener.Close();
        }
        private async void HandleRequests()
        {
            while (listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    string requestUrl = context.Request.Url.AbsolutePath;
                    if (requestUrl == "/ping")
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.Close();
                    }
                }
                catch { }
            }
        }
    }
}
