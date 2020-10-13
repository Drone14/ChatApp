using Connections;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ChatAppGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public delegate void DisplayCallback(string s);
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .Build();

            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(config["localEP:ip"]), Convert.ToInt32(config["localEP:port"]));
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(config["remoteEP:ip"]), Convert.ToInt32(config["remoteEP:port"]));
            byte[] key = HexStringToByteArray(config["AESkey"]);

            Task.Run(() =>
            {
                if (!Connection.Init(localEP, remoteEP, 2, 512, key, Display))
                    return;
                SendButton.Dispatcher.Invoke(() => SendButton.IsEnabled = true);
            } );
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
        public void OnSendClick(object sender, RoutedEventArgs e)
        {
            SentBox.Text += (MessageBox.Text + '\n');
            Connection.Send(MessageBox.Text);
            MessageBox.Clear();
        }
        public void OnEncryptClick(object sender, RoutedEventArgs e)
        {
            DateTime start = DateTime.Now;
            using (Aes alg = Aes.Create())
            {
                alg.Key = HexStringToByteArray(KeyTextBox.Text);
                ICryptoTransform encryptor = alg.CreateEncryptor();
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            swEncrypt.Write(EncryptInput.Text);
                        EncryptOutput.Text = Encoding.UTF8.GetString(msEncrypt.ToArray());
                    }
                }
            }
            TimeSpan elapsed = DateTime.Now - start;
            TimeBox.Text = elapsed.TotalMilliseconds.ToString() + "ms";
        }
        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Connection.Close();
        }
    }
}
