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

            IPEndPoint localEP = new IPEndPoint(Dns.GetHostEntry("localhost").AddressList[0], Convert.ToInt32(config["endpoint:port"]));
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(config["endpoint:ip"]), Convert.ToInt32(config["endpoint:port"]));

            bool initialized = Connection.Init(localEP, remoteEP, 2, 128, Display);
            Connection.Close();
            Console.WriteLine("Initialized: {0}", initialized);
        }

        static void Display(string s)
        {
            Console.WriteLine(s);
        }
    }
}
