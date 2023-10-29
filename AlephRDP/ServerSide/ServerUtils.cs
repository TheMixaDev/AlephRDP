using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.IO;

namespace AlephRDP.ServerSide
{
    internal class ServerUtils
    {
        public static void SendNotification(string caption, string message)
        {
            if (!Server.Instance.Notify) return;
            new ToastContentBuilder()
                .AddText(caption)
                .AddText(message)
                .Show();
        }
        public static void Log(String message)
        {
            File.AppendAllText("connection.log", $"{DateTime.Now.ToString("MM.dd")} [{DateTime.Now.ToString("HH:mm:ss")}] {message}{Environment.NewLine}");
        }
    }
}
