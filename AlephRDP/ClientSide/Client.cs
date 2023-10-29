using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using WebSocketSharp;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net.Http;

namespace AlephRDP.ClientSide
{
    public class Client
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private WebSocket webSocket;
        private bool authorized = false;
        private string redirect = null;
        private string redirectData = null;

        private string ip;
        private int port;
        private string password;
        private ClientStatus status = ClientStatus.Disconnected;
        private string statusInfo = "";

        private ClientScreenshare screenshare;
        private ClientConsole console;

        public Client(string ip, int port, string password)
        {
            this.ip = ip;
            this.port = port;
            this.password = password;

            screenshare = new ClientScreenshare(this);
            console = new ClientConsole(this);
        }

        public WebSocket WebSocket { get => webSocket; }
        public ClientScreenshare ClientScreenshare { get => screenshare; }
        public ClientConsole ClientConsole { get => console; }
        public ClientStatus Status { get => status; set => SetField(ref status, value); }
        public string StatusInfo { get => statusInfo; set => SetField(ref statusInfo, value); }
        public string Ip { get => ip; }
        public bool Authorized { get => authorized; set => SetField(ref authorized, value); }
        public static async void Ping(string ip, int port, Action successCallback, Action failCallback)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync($"http://{ip}:{port + 1}/ping");
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        successCallback.Invoke();
                        return;
                    }
                } catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
            }
            failCallback.Invoke();
            return;
        }
        public void Connect()
        {
            Status = ClientStatus.Pinging;
            Ping(ip, port, new Action(() =>
            {
                ConnectWS();
            }), new Action(() =>
            {
                Status = ClientStatus.Disconnected;
            }));
        }
        private void ConnectWS()
        {
            Status = ClientStatus.Connecting;
            ip = ip+":"+port.ToString();
            webSocket = new WebSocket($"ws://{ip}/Computer");
            webSocket.OnMessage += (sender, e) =>
            {
                if (redirect != null)
                {
                    try
                    {
                        if (redirect == "Image")
                            screenshare.ImageFromBytes(e.RawData);
                        if (redirect == "File")
                        {
                            if (!Directory.Exists("savedFiles"))
                                Directory.CreateDirectory("savedFiles");
                            File.WriteAllBytes("savedFiles/" + redirectData, e.RawData);
                            Process.Start("explorer.exe", "savedFiles");
                        }
                    }
                    catch { }
                    redirect = null;
                    return;
                }
                JObject response;
                try
                {
                    response = JsonConvert.DeserializeObject<JObject>(e.Data);
                }
                catch
                {
                    Status = ClientStatus.Error;
                    StatusInfo = "Incorrect data type recieved";
                    return;
                }
                if (Convert.ToInt32(response["Status"]) == 401 && !authorized)
                {
                    SendJSON(new { Data = password });
                    authorized = true;
                }
                else if (Convert.ToInt32(response["Status"]) == 401 && authorized)
                    authorized = false;
                else if (Convert.ToInt32(response["Status"]) == 302)
                {
                    redirect = response["Destination"].ToString();
                    if (response.ContainsKey("Name"))
                        redirectData = response["Name"].ToString();
                }
                else if (Convert.ToInt32(response["Status"]) == 200)
                {
                    if (response.ContainsKey("LabeledStatus"))
                    {
                        Status = ClientStatus.Labeled;
                        StatusInfo = response["LabeledStatus"].ToString();
                    }
                    if (response.ContainsKey("App"))
                        if (response["App"].ToString() == "Console")
                        {
                            console.Output = response["Output"].ToString();
                            console.Updates++;
                        }
                        else if (response["App"].ToString() == "Screen")
                            screenshare.ScreenCount = Convert.ToInt32(response["ScreenCount"]);
                }
            };
            webSocket.OnClose += (sender, e) =>
            {
                authorized = false;
                Status = ClientStatus.Disconnected;
                Console.WriteLine("Connection closed.");
                webSocket.Close();
            };
            webSocket.OnOpen += (sender, e) =>
            {
                Status = ClientStatus.Connected;
            };
            webSocket.Connect();
        }
        public void Disconnect()
        {
            if (webSocket != null && webSocket.IsAlive) webSocket.Close();
        }
        public void SendJSON(object value)
        {
            webSocket.Send(JsonConvert.SerializeObject(value));
        }
    }
}
