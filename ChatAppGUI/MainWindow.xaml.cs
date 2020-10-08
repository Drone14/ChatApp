using Connections;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Windows;

namespace ChatAppGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public delegate void DisplayCallback(string);
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .Build();

            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(config["localEP:ip"]), Convert.ToInt32(config["localEP:port"]));
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(config["remoteEP:ip"]), Convert.ToInt32(config["remoteEP:port"]));
            byte[] key = HexStringToByteArray(config["AESkey"]);

            if (!Connection.Init(localEP, remoteEP, 2, 256, key, Display))
                return;

            InitializeComponent();
        }
        private byte[] HexStringToByteArray(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];

            for (int i = 0; i < hex.Length; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
        }
        public void Display(string s)
        {
            DisplayBox.Dispatcher.Invoke(new DisplayCallback(DisplayCB), new object[] { s });
        }
        public void DisplayCB(string s)
        {
            DisplayBox.Text += (s + '\n');
        }
        public void OnClick(object sender, RoutedEventArgs e)
        {
            Connection.Send(MessageBox.Text);
            MessageBox.Clear();
        }
        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Connection.Close();
        }
    }
}
