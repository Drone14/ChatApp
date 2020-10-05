using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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
        private static byte[] receiveBuffer;
        private static byte[] sendBuffer;

        //Delegate to hold method for displaying messages; to be provided by client program
        private static DisplayMethod Display;

        //Used to cause the main thread to wait for the accepting process to complete
        private static readonly ManualResetEvent done = new ManualResetEvent(false);

        //Client program passes IPEndpoints to class, then class configures listener and sender sockets; If fail, return false
        public static bool Init(IPEndPoint local, IPEndPoint remote, int queueSize, int bufferLength, DisplayMethod method)
        {
            //This method makes Debug.Writeline() write to console when built in debug configuration
            Trace.Listeners.Add(new TextWriterTraceListener(System.Console.Out));

            receiveBuffer = new byte[bufferLength];
            sendBuffer = new byte[bufferLength];
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

            //Get remote connection to assign to accept
            try
            {
                //Begin listening
                listener.Bind(local);
                listener.Listen(queueSize);
                Debug.WriteLine("Listening on " + listener.LocalEndPoint.ToString() + "...");

                //Accepting must be done asynchronously so that thread wont be blocked here; AcceptCallback will block on EndAccept method until a connection is available to accept
                Debug.WriteLine("Beginning asynchronous accepting procedure...");
                listener.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
            catch(Exception e)
            {
                Debug.WriteLine("Failed to accept remote connection: {0}", e.ToString());
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

            //Begin asynchronous recieving procedure
            Debug.WriteLine("Beginning asynchronous receiving procedure...");
            accept.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);

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

        private static void ReceiveCallback(IAsyncResult ar)
        {
            int bytesReceived;
            try
            {
                bytesReceived = accept.EndReceive(ar);
                Debug.WriteLine("{0} bytes received", bytesReceived);

                //Display the message and clear the buffer
                if (bytesReceived > 0) //If bytes were read
                {
                    Display(Encoding.ASCII.GetString(receiveBuffer));
                    Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
                    accept.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
                }
                else
                {
                    Debug.WriteLine("Accept socket connetion ended... Closing accept socket");
                    accept.Shutdown(SocketShutdown.Both);
                    accept.Close();
                }
            }
            catch(ObjectDisposedException)
            {
                Debug.WriteLine("Receive process stopping because accept socket has been closed");
            }
        }

        public static void Send(string s)
        {
            sendBuffer = Encoding.ASCII.GetBytes(s);
            sender.Send(sendBuffer);
            Array.Clear(sendBuffer, 0, sendBuffer.Length);
        }

        public static void Close()
        {
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
            if (accept.Connected)
            {
                accept.Shutdown(SocketShutdown.Both);
                accept.Close();
            }
        }
    }
}