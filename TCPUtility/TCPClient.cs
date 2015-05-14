/*  Written by Rasmus Jönsson (www.rasmusj.se)
 *  Created 2015-05-14
 *  Licensed under MIT License
*/

using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace TCPUtility
{
    //Security types
    public enum TCPSecurity { Plain, Encrypted, EncryptedAndVerified };

    public class TCPClient
    {

        //Objects for bort encrypted/plain
        private TcpClient client;
        private NetworkStream stream;
        private SslStream secureStream;
        
        //Thread to enable full duplex
        private Thread listenThread;

        private string hostname;
        private TCPSecurity security;
        private int port;

        //Standard intialization
        public TCPClient(string hostname, int port, TCPSecurity security = TCPSecurity.Plain)
        {
            this.hostname = hostname;
            this.security = security;
            this.port = port;
        }

        //For usage in TCPServer class. Therefore it is internal
        internal TCPClient(TcpClient tcpClient, int port)
        {
            this.client = tcpClient;
            //Assume plain as no certificate is attatched to server
            this.security = TCPSecurity.Plain;
            //Set hostname to the client ip address (Not needed but helps to identify clients in your application)
            IPEndPoint ipep = (IPEndPoint)(tcpClient.Client.RemoteEndPoint);
            IPAddress ipa = ipep.Address;
            hostname = ipa.ToString();
            stream = client.GetStream();
            //Start receiving thread
            listenThread = new Thread(new ThreadStart(ListenForMessages));
            listenThread.Start();
        }

        //If a certificate is added to the initialization of the server we automatically enable encryption
        internal TCPClient(TcpClient tcpClient, int port, X509Certificate2 certificate)
        {
            this.client = tcpClient;
            //Certificate attached, assume encrypted. (No need to have + verified here because server dont verify clients anyway)
            this.security = TCPSecurity.Encrypted;
            IPEndPoint ipep = (IPEndPoint)(tcpClient.Client.RemoteEndPoint);
            IPAddress ipa = ipep.Address;
            hostname = ipa.ToString();
            //Add security to our stream
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateCertificate);
            secureStream = new SslStream(tcpClient.GetStream());
            //Authenticate
            secureStream.AuthenticateAsServer(certificate, false, System.Security.Authentication.SslProtocols.Default, false);
            listenThread = new Thread(new ThreadStart(ListenForMessages));
            listenThread.Start();
        }

        //Get- and seters

        public string Hostname { get { return hostname; } }

        public int Port { get { return port; } }

        public bool Connected
        {
            get { return client.Connected; }
        }

        public TCPSecurity TCPSecurity { get { return security; } }

        //Event handler for incoming messages
        public event EventHandler<MessageReceivieEventArgs> MessageReceived;

        //Establish connection the the hostname and port
        public void Connect()
        {
            try {
                client = new TcpClient(hostname, port);
                stream = client.GetStream();
                
                ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateCertificate);
                //If secure add security to stream
                if (security == TCPSecurity.Encrypted)
                        secureStream = new SslStream(stream, false, new RemoteCertificateValidationCallback(ValidateCertificate));

                if (security == TCPSecurity.EncryptedAndVerified)
                    secureStream = new SslStream(stream, false);

                if (security != TCPSecurity.Plain)
                {
                    //authenticate
                    secureStream.AuthenticateAsClient(hostname);
                }

                //Listen for incomig messages
                listenThread = new Thread(new ThreadStart(ListenForMessages));
                listenThread.Start();

             }
             catch (Exception e)
             {
                 throw new ClientException(e.Message);
             }
        }

        //Used to bypass trusted root validation if wanted
        private bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        //Listen for incoming messages
        private void ListenForMessages()
        {
           try
           {
                //Block while connected
                while (client.Connected)
                {
                    MessageReceivieEventArgs args = new MessageReceivieEventArgs();
                    byte[] data = new byte[4096]; //Buffer size is 4096 bytes 
                    int bytes = 0;
                    //if security is enabled use secure stream instead
                    if (security != TCPSecurity.Plain)
                        bytes = secureStream.Read(data, 0, data.Length);
                    else
                        bytes = stream.Read(data, 0, data.Length);
                    args.Message = Encoding.Unicode.GetString(data, 0, bytes);
                    OnMessageReceive(args);
                }
            }
            catch
            {
                //Do nothing at the moment, TODO: Add better errorhandling
            }
        }

        protected virtual void OnMessageReceive(MessageReceivieEventArgs e)
        {
            EventHandler<MessageReceivieEventArgs> handler = MessageReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        //Some connect overloads (If you wish to use the same client for another destination easily

        public void Connect(string hostname)
        {
            this.hostname = hostname;
            Connect();
        }

        public void Connect(string hostname, int port)
        {
            this.hostname = hostname;
            this.port = port;
            Connect();
        }

        //Method to send messages to this client
        public void Send(string message)
        {
            if (!Connected)
                throw new ClientException("Client is not connected to server!");
            Byte[] data = Encoding.Unicode.GetBytes(message);
            //If security is enabled use secure stream
            if (security != TCPSecurity.Plain)
            {
                secureStream.Write(data, 0, data.Length);
                secureStream.Flush();
            }
            else
            { 
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }
        }

        //Close connection and stream if they exist
        public void Close()
        {
            if (stream != null)
                stream.Close();
            if (client != null)
                client.Close();
        }
    }

    //This is the eventarg object
    public class MessageReceivieEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    //Custom exception for client related errors
    public class ClientException : Exception
    {
        public ClientException()
        {
        }

        public ClientException(string message)
            : base(message)
        {
        }

        public ClientException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

}
