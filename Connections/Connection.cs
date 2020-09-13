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
        private static Socket listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
        private static Socket sender = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
        private static Socket accept = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
        private static byte[] recieveBuffer;
        private static int listenQueueSize;
        private static int bufferSize;

        //Delegate to hold method for displaying messages; to be provided by client program
        private static DisplayMethod Display;

        //Client program passes IPEndpoints to class, then class configures listener and sender sockets; If fail, return false
        public static bool Init(IPEndPoint local, IPEndPoint remote, int queueSize, int bufferLength, DisplayMethod method)
        {
            //This method makes Debug.Writeline() write to console when built in debug configuration
            Trace.Listeners.Add(new TextWriterTraceListener(System.Console.Out));

            //Used to cause the main thread to wait for the accepting process to complete
            WaitHandle acceptProcess;

            listenQueueSize = queueSize;
            bufferSize = bufferLength;
            Display = method;

            try
            {
                //Begin listening
                listener.Bind(local);
                listener.Listen(listenQueueSize);
                Debug.WriteLine("Listening on " + listener.LocalEndPoint.ToString() + "...");

                //Accepting and recieving must be done asynchronously so that thread wont be blocked here; AcceptCallback will block on EndAccept method until a connection is available to accept
                Debug.WriteLine("Beginning asynchronous accepting procedure...");

                //Begin asynchronous accepting process and store the WaitHandle
                acceptProcess = listener.BeginAccept(AcceptCallback, null).AsyncWaitHandle;
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
            acceptProcess.WaitOne();

            return true;
        }

        //Method that will be called when connection is accepted
        private static void AcceptCallback(IAsyncResult ar)
        {
            //Assign accepted remote connection to accept
            accept = listener.EndAccept(ar);
            Debug.WriteLine("Connection accepted");
        }

        public static void Close()
        {
            //listener.Shutdown(SocketShutdown.Both); Removed because listener is never connected remotely
            listener.Close();
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
            accept.Shutdown(SocketShutdown.Both);
            accept.Close();
        }
    }
}
