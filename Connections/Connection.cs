﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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
        private static byte[] key;

        //Delegate to hold method for displaying messages; to be provided by client program
        private static DisplayMethod Display;

        //Used to cause the main thread to wait for the accepting process to complete
        private static readonly ManualResetEvent done = new ManualResetEvent(false);
        private static readonly ManualResetEvent sending = new ManualResetEvent(true);
        private static readonly ManualResetEvent receiving = new ManualResetEvent(true);

        //Client program passes IPEndpoints to class, then class configures listener and sender sockets; If fail, return false
        public static bool Init(IPEndPoint local, IPEndPoint remote, int queueSize, int bufferLength, byte[] k, DisplayMethod method)
        {
            //This method makes Debug.Writeline() write to console when built in debug configuration
            Trace.Listeners.Add(new TextWriterTraceListener(System.Console.Out));

            receiveBuffer = new byte[bufferLength];
            sendBuffer = new byte[bufferLength];

            //Assign key
            key = new byte[k.Length];
            Array.Copy(k, key, k.Length);

            //Subscribe the client's display method to the Display delegate
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
        public static void Send(string s)
        {
            sending.Reset();
            sendBuffer = Encrypt(s);

            //Note that the length of message is restricted by the size of the buffer provided by the client program
            sender.BeginSend(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);
        }
        //Method that will be called when connection is accepted
        private static void AcceptCallback(IAsyncResult ar)
        {
            //Assign accepted remote connection to accept
            try
            {
                accept = listener.EndAccept(ar);
                listener.Close();
                Debug.WriteLine("Accept socket connected");
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
            byte[] bytes;

            try
            {
                bytesReceived = accept.EndReceive(ar);
                receiving.Reset();
                Debug.WriteLine("{0} bytes received", bytesReceived);

                //Copy the data received to a new array
                bytes = new byte[bytesReceived];
                Array.Copy(receiveBuffer, 0, bytes, 0, bytes.Length);

                //Display the message and clear the buffer
                if (bytesReceived > 0) //If bytes were read
                {
                    //Display(Encoding.ASCII.GetString(receiveBuffer).Trim('\0'));
                    Display(Decrypt(bytes));
                    Array.Clear(receiveBuffer, 0, receiveBuffer.Length);
                    receiving.Set();
                    accept.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
                }
                else
                {
                    Debug.WriteLine("Accept socket connetion ended... Closing accept socket");
                    accept.Shutdown(SocketShutdown.Both);
                    accept.Close();
                    receiving.Set();
                }
            }
            catch(ObjectDisposedException)
            {
                Debug.WriteLine("Receive process stopping because accept socket has been closed");
            }
        }
        private static void SendCallback(IAsyncResult ar)
        {
            sender.EndSend(ar);
            Debug.WriteLine("SendCallback finished executing");
            sending.Set();
        }
        //Returns the output of encryption of the string with a prepended IV
        private static byte[] Encrypt(string s)
        {
            byte[] encrypted;
            byte[] msArray;

            using(Aes alg = Aes.Create())
            {
                alg.Key = key;

                ICryptoTransform enc = alg.CreateEncryptor();
                using (MemoryStream msEnc = new MemoryStream())
                {
                    using (CryptoStream csEnc = new CryptoStream(msEnc, enc, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEnc = new StreamWriter(csEnc))
                            swEnc.Write(s);
                        msArray = msEnc.ToArray();
                    }
                }
                encrypted = new byte[alg.IV.Length + msArray.Length];
                alg.IV.CopyTo(encrypted, 0);
                msArray.CopyTo(encrypted, alg.IV.Length);
            }

            return encrypted;
        }
        private static string Decrypt(byte[] b)
        {
            string plaintext;

            using (Aes alg = Aes.Create())
            {
                alg.Key = key;
                byte[] ivector = new byte[alg.IV.Length];
                byte[] bytes = new byte[b.Length - alg.IV.Length];

                //Copy the IV to ivector and copy the data to bytes
                Array.Copy(b, 0, ivector, 0, alg.IV.Length);
                Array.Copy(b, alg.IV.Length, bytes, 0, bytes.Length);

                //Give the IV to Aes object
                alg.IV = ivector;

                ICryptoTransform dec = alg.CreateDecryptor();
                using (MemoryStream msDec = new MemoryStream(bytes))
                {
                    using (CryptoStream csDec = new CryptoStream(msDec, dec, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDec = new StreamReader(csDec))
                            plaintext = srDec.ReadToEnd();
                    }
                }
            }

            return plaintext;
        }
        public static void Close()
        {
            sending.WaitOne();
            receiving.WaitOne();

            if (sender.Connected)
            {
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
            }
            if (accept.Connected)
            {
                accept.Shutdown(SocketShutdown.Both);
                accept.Close();
            }
        }
    }
}