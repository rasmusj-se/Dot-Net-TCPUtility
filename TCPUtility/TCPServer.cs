using System;
/*  Written by Rasmus Jönsson (www.rasmusj.se)
 *  Created 2015-05-14
 *  Licensed under MIT License
*/

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography.X509Certificates;

namespace TCPUtility
{
    public class TCPServer
    {

        private TcpListener tcpListener;
        private Thread listenThread;
        private TCPSecurity security;
        private X509Certificate2 cert;

        private int port;

        public TCPServer(int port)
        {
            this.port = port;
            this.security = TCPSecurity.Plain;
        }

        //Certificate passed set encryption true. Passed certificate is in pfx format
        public TCPServer(int port, string certificate, string password)
        {
            this.port = port;
            this.security = TCPSecurity.Encrypted;
            cert = new X509Certificate2(certificate, password);
            
        }

        //Start TcpListener
        public void Start()
        {
            this.tcpListener = new TcpListener(IPAddress.Any, port);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        //Listen and await clients
        private void ListenForClients()
        {
            this.tcpListener.Start();
            while (true)
            {
                TcpClient client = this.tcpListener.AcceptTcpClient();
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
        }

        //Eventhandler for clientconnected to server
        public event EventHandler<ClientConnectEventArgs> ClientConnect;

        private void HandleClientComm(object client)
        {
            //Create a new networkclientservice between server and client and then pass this service to the eventhandler
            ClientConnectEventArgs args = new ClientConnectEventArgs();
            if (security != TCPSecurity.Plain)
                args.NetworkClientService = new TCPClient((TcpClient)client, port, cert);
            else
                args.NetworkClientService = new TCPClient((TcpClient)client, port);
            OnClientConnect(args);
        }

        protected virtual void OnClientConnect(ClientConnectEventArgs e)
        {
            EventHandler<ClientConnectEventArgs> handler = ClientConnect;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        //Stop TcpListener
        public void Stop()
        {
            tcpListener.Stop();
        }
    }

    public class ClientConnectEventArgs : EventArgs
    {
        public TCPClient NetworkClientService { get; set; }
    }
}
