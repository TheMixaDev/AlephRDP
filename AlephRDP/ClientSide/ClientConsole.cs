using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace AlephRDP.ClientSide
{
    public class ClientConsole
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

        private Client client;
        private string output;
        private long updates = 0;
        public ClientConsole(Client client)
        {
            this.client = client;
        }

        public string Output { get => output; set => output = value; }
        public long Updates { get => updates; set => SetField(ref updates, value); }

        public void OpenConsole()
        {
            client.SendJSON(new { App = "Console" });
        }
        public void ProcessInputCommand(string input)
        {
            client.SendJSON(new { App = "Console", Command = input });
        }
        public bool UploadFile(string fileName)
        {
            FileInfo file = new FileInfo(fileName.Trim());
            if (!file.Exists)
            {
                client.SendJSON(new { App = "Console", Command = "cd ." });
                return false;
            }
            client.SendJSON(new { App = "Upload", Path = file.Name });
            client.WebSocket.Send(file);
            client.SendJSON(new { App = "Console", Command = "cd ." });
            return true;
        }
    }
}
