/*  Written by Rasmus Jönsson (www.rasmusj.se)
 *  Created 2015-05-14
 *  Licensed under MIT License
*/

using System;
using System.Collections.Generic;

namespace TCPUtility.ChatServer
{
    class Program
    {
        //Create client list and server
        static List<TCPClient> clients;
        static TCPServer server;

        static void Main(string[] args)
        {
            //Initialize clientlist
            clients = new List<TCPClient>();

            //Initialize and start server on port 1337 with SSL encryption (pass a pfx file and a password) Make sure certificate file is accessiable
            //To disable server encryption simply use server = new TCPServer(1337); instead (Dont forget to set TCPSecurity.Plain on client also)
            server = new TCPServer(1337, "democert.pfx", "password");
            server.Start();
            //Attatch a client connect handler
            server.ClientConnect += Server_ClientConnect;

            //Stall console 
            Console.WriteLine("Server started. Press any key to shutdown server.");
            Console.ReadKey();
            
            //Close each client gracefully
            foreach (TCPClient client in clients)
                client.Close();
            //Close server gracefully
            server.Stop();
        }

        private static void Server_ClientConnect(object sender, ClientConnectEventArgs e)
        {
            TCPClient client = e.NetworkClientService;
            //Attatch message receive eventhandler
            client.MessageReceived += Client_MessageReceived;
            //Make client aware that it is connected
            client.Send("You are connected to the chatserver.");
            //Add client to clientlist
            clients.Add(client);
        }


        private static void Client_MessageReceived(object sender, MessageReceivieEventArgs e)
        {
            //Display chatlog in server console (This is ofcourse not needed)
            Console.WriteLine("Received: {");
            Console.WriteLine(e.Message);
            Console.WriteLine("} from " + ((TCPClient)sender).Hostname + "(" + ((TCPClient)sender).TCPSecurity+ ")");
            //Send message to all clients
            foreach (TCPClient client in clients)
                client.Send(((TCPClient)sender).Hostname + ": " + e.Message);
        }

    }
}
