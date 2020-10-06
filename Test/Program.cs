using System;
using System.Net;
using Connections;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, false)
                .Build();

            IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(config["localEP:ip"]), Convert.ToInt32(config["localEP:port"]));
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(config["remoteEP:ip"]), Convert.ToInt32(config["remoteEP:port"]));
            byte[] key = HexStringToByteArray("5468617473206D79204B756E67204675");

            if (!Connection.Init(localEP, remoteEP, 2, 256, key, Display))
                return;

            Connection.Send("Test message");
            Thread.Sleep(3000);
            Connection.Close();
            return;
        }
        public static byte[] HexStringToByteArray(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];

            for(int i = 0; i < hex.Length; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            return bytes;
        }
        public static void Display(string s)
        {
            Console.WriteLine(s);
        }
    }
}
