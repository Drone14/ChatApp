using System;
using System.Net;
using Connections;
using Microsoft.Extensions.Configuration;
using System.IO;

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

            if (!Connection.Init(localEP, remoteEP, 2, 256, Display))
                return;

            Connection.Send("Test message");

            Connection.Close();
            return;
        }

        static void Display(string s)
        {
            Console.WriteLine(s);
        }
    }
}
