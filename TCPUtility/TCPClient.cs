using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace TCPUtility
{
    public enum TCPSecurity { Plain, Encrypted, EncryptedAndVerified };

    public class TCPClient
    {

        private TcpClient client;
        private NetworkStream stream;
        private SslStream secureStream;
        
        //Thread to enable full duplex
        private Thread listenThread;

        private string hostname;
        private TCPSecurity security;
        private int port;

        public TCPSecurity TCPSecurity { get { return security; } }

        public TCPClient(string hostname, int port, TCPSecurity security = TCPSecurity.Plain)
        {
            this.hostname = hostname;
            this.security = security;
            this.port = port;
        }

        //For usage in server
        internal TCPClient(TcpClient tcpClient, int port)
        {
            this.client = tcpClient;
            this.security = TCPSecurity.Plain;
            IPEndPoint ipep = (IPEndPoint)(tcpClient.Client.RemoteEndPoint);
            IPAddress ipa = ipep.Address;
            hostname = ipa.ToString();
            stream = client.GetStream();
            listenThread = new Thread(new ThreadStart(ListenForMessages));
            listenThread.Start();
        }

        internal TCPClient(TcpClient tcpClient, int port, X509Certificate2 certificate)
        {
            this.client = tcpClient;
            this.security = TCPSecurity.Encrypted;
            IPEndPoint ipep = (IPEndPoint)(tcpClient.Client.RemoteEndPoint);
            IPAddress ipa = ipep.Address;
            hostname = ipa.ToString();
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateCertificate);
            secureStream = new SslStream(tcpClient.GetStream());
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

        //Event handler for incoming messages
        public event EventHandler<MessageReceivieEventArgs> MessageReceived;

        //Establish connection the the TcpEndpoint
        public void Connect()
        {
            try {
                client = new TcpClient(hostname, port);
                stream = client.GetStream();
                
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateCertificate);
            if (security == TCPSecurity.Encrypted)
                    secureStream = new SslStream(stream, false, new RemoteCertificateValidationCallback(ValidateCertificate));

                if (security == TCPSecurity.EncryptedAndVerified)
                    secureStream = new SslStream(stream, false);

               if (security != TCPSecurity.Plain)
               {
                
                    secureStream.AuthenticateAsClient(hostname);
               }

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
                    byte[] data = new byte[4096];
                    int bytes = 0;
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

        //Some connect overloads

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

        //Close connection and stream
        public void Close()
        {
            if (stream != null)
                stream.Close();
            if (client != null)
                client.Close();
        }
    }

    public class MessageReceivieEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

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
