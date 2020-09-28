using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Connections
{
    public delegate void DisplayMethod(string s);
    public static class Connection
    {
        private static Socket listener;
        private static Socket sender;
        private static Socket accept;
        private static byte[] recieveBuffer;
        private static int listenQueueSize;
        private static int bufferSize;

        //Delegate to hold method for displaying messages; to be provided by client program
        private static DisplayMethod Display;

        //Used to cause the main thread to wait for the accepting process to complete
        private static ManualResetEvent done = new ManualResetEvent(false);

        //Client program passes IPEndpoints to class, then class configures listener and sender sockets; If fail, return false
        public static bool Init(IPEndPoint local, IPEndPoint remote, int queueSize, int bufferLength, DisplayMethod method)
        {
            //This method makes Debug.Writeline() write to console when built in debug configuration
            Trace.Listeners.Add(new TextWriterTraceListener(System.Console.Out));

            listenQueueSize = queueSize;
            bufferSize = bufferLength;
            Display = method;

            //Create listener
            try
            {
                listener = new Socket(local.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Debug.WriteLine("Listener socket instantiated");
            }
            catch(Exception e)
            {
                Debug.WriteLine("Failed to create listener: {0}", e.ToString());
                return false;
            }
            
            //Create sender socket
            try
            {
                sender = new Socket(remote.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Debug.WriteLine("Sender socket instantiated");
            }
            catch(Exception e)
            {
                Debug.WriteLine("Failed to create sender: {0}", e.ToString());
                return false;
            }
            
            //Create accept socket
            try
            {
                accept = new Socket(remote.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                Debug.WriteLine("Accept socket instantiated");
            }
            catch(Exception e)
            {
                Debug.WriteLine("Failed to create accept: {0}", e.ToString());
                return false;
            }

            try
            {
                //Begin listening
                listener.Bind(local);
                listener.Listen(listenQueueSize);
                Debug.WriteLine("Listening on " + listener.LocalEndPoint.ToString() + "...");

                //Accepting and recieving must be done asynchronously so that thread wont be blocked here; AcceptCallback will block on EndAccept method until a connection is available to accept
                Debug.WriteLine("Beginning asynchronous accepting procedure...");

                //Begin asynchronous accepting process and store the WaitHandle
                listener.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch(Exception e)
            {
                Debug.WriteLine("Failed to listen: {0}", e.ToString());
                return false;
            }

            //Establish connection with remote EP
            try
            {
                Debug.WriteLine("Connecting to remote socket...");
                sender.Connect(remote);
                Debug.WriteLine("Connected to remote socket");
            }
            catch(Exception e)
            {
                Debug.WriteLine("Failed to connect to remote EP: {0}", e.ToString());
                return false;
            }

            //Wait until the remote connection has been accepted
            done.WaitOne();

            return true;
        }

        //Method that will be called when connection is accepted
        private static void AcceptCallback(IAsyncResult ar)
        {
            //Assign accepted remote connection to accept
            try
            {
                accept = listener.EndAccept(ar);
                listener.Close();
            }
            catch(Exception e)
            {
                Debug.WriteLine("Failed to accept connection: {0}", e.ToString());
            }
            
            Debug.WriteLine("Connection accepted");
            done.Set();
        }

        public static void Close()
        {
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
            accept.Shutdown(SocketShutdown.Both);
            accept.Close();
        }
    }
}