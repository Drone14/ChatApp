using System;
using System.Net;
using Connections;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            IPHostEntry iPHostEntry = Dns.GetHostEntry("localhost");
            IPAddress iPAddress = iPHostEntry.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(iPAddress, 11000);

            bool initialized = Connection.Init(endPoint, endPoint, 2, 128, Display);
            Connection.Close();
            Console.WriteLine("Initialized: {0}", initialized);
        }

        static void Display(string s)
        {
            Console.WriteLine(s);
        }
    }
}
