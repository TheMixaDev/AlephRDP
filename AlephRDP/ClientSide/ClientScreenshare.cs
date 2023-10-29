using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AlephRDP.ClientSide
{
    public class ClientScreenshare
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
        private int screenCount = 1;
        private int screen = 0;
        private Image currentImage;
        public ClientScreenshare(Client client) {
            this.client = client;
        }

        public Image CurrentImage { get => currentImage; set => SetField(ref currentImage, value); }
        public int ScreenCount { get => screenCount; set => screenCount = value; }

        public void OpenScreenshare()
        {
            client.SendJSON(new { App = "Screen", Notify = "Connected" });
            RequestUpdate();
        }
        public void RequestUpdate()
        {
            client.SendJSON(new { App = "Screen", Screen = screen });
        }
        public void CloseScreenshare()
        {
            client.SendJSON(new { App = "Screen", Notify = "Disconnected" });
        }
        public void ImageFromBytes(byte[] data)
        {
            try
            {
                byte[] imageData = data;
                Image image;
                using (MemoryStream ms = new MemoryStream(imageData))
                {
                    image = Image.FromStream(ms);
                }
                CurrentImage = image;
            }
            catch { }
        }
    }
}
