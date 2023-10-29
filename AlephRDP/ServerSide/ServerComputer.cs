using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.IO;
using WebSocketSharp.Server;
using WebSocketSharp;

namespace AlephRDP.ServerSide
{
    internal class ServerComputer : WebSocketBehavior
    {
        public ServerConsole serverConsole = new ServerConsole();
        private bool authorized = false;
        private string upload = null;
        protected override void OnMessage(MessageEventArgs e)
        {
            if (upload != null)
            {
                Log.Info("Contents of " + upload);
                File.WriteAllBytes(serverConsole.workingDirectory + "/" + upload, e.RawData);
                File.AppendAllText("console.log", "upload " + serverConsole.workingDirectory + "/" + upload + Environment.NewLine + $"{serverConsole.workingDirectory}> ");
                upload = null;
                return;
            }
            Log.Info(e.Data);
            JObject request;
            try
            {
                request = JsonConvert.DeserializeObject<JObject>(e.Data);
            }
            catch
            {
                SendJSON(new { Status = 404 });
                Sessions.CloseSession(ID, CloseStatusCode.UnsupportedData, "Incorrect data type recieved");
                return;
            }
            if (!authorized)
            {
                if (request.ContainsKey("Data") && request["Data"].ToString() == Server.Instance.Password)
                {
                    ServerUtils.SendNotification("AlephRDP", "Новое соединение");
                    ServerUtils.Log($"Connection {this.Context.UserEndPoint} opened");
                    authorized = true;
                    SendJSON(new { LabeledStatus = "Authorized", Status = 200 });
                }
                else
                {
                    SendJSON(new { Status = 401 });
                    Sessions.CloseSession(ID, CloseStatusCode.InvalidData, "Incorrect password");
                }
                return;
            }
            if (request.ContainsKey("App"))
            {
                if (request["App"].ToString() == "Console")
                {
                    if (request.ContainsKey("Command"))
                    {
                        string output = serverConsole.RunCommand(request["Command"].ToString());
                        if (output != null)
                            SendJSON(new { App = "Console", Output = output, Status = 200 });
                        else
                        {
                            SendJSON(new { Destination = "File", Name = serverConsole.toSend.Name, Status = 302 });
                            Send(serverConsole.toSend);
                            SendJSON(new { App = "Console", Output = serverConsole.RunCommand("cd ."), Status = 200 });
                        }
                        return;
                    }
                    else
                    {
                        serverConsole.RunCommand("reset");
                        SendJSON(new { App = "Console", Output = serverConsole.workingDirectory + "> ", Status = 200 });
                        return;
                    }
                }
                else if (request["App"].ToString() == "Screen")
                {
                    if (request.ContainsKey("Notify"))
                    {
                        ServerUtils.Log($"Connection {this.Context.UserEndPoint} changed screensharing status: {request["Notify"].ToString()}");
                        return;
                    }
                    SendJSON(new { App = "Screen", ScreenCount = ServerScreenshare.GetScreenCount(), Status = 200 });
                    SendJSON(new { Destination = "Image", Status = 302 });
                    ServerScreenshare.CreateScreenshot(Convert.ToInt32(request["Screen"]));
                    Send(new FileInfo("screenshot.png"));
                    File.Delete("screenshot.png");
                }
                else if (request["App"].ToString() == "Upload")
                {
                    upload = request["Path"].ToString();
                }
            }
            SendJSON(new { Status = 404 });
        }
        protected override void OnOpen()
        {
            base.OnOpen();
            SendJSON(new { Type = "Auth", Status = 401 });
            Log.Info($"Connection {ID} opened. IP: {Context.Host}, {Context.Origin}");
        }
        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            Log.Info($"Connection {ID} closed with code {e.Code} and reason {e.Reason}");
            if (authorized)
            {
                ServerUtils.SendNotification("AlephRDP", "Соединение прекращено");
                ServerUtils.Log($"Connection closed");
            }
        }

        public void SendJSON(object value)
        {
            Send(JsonConvert.SerializeObject(value));
        }
    }
}
