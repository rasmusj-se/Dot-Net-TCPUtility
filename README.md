##TCP Utility written in C#. 

Support both encrypted and non encrypted communication, full duplex and multiple clients. 
Uses eventhandlers to announce connected client or incoming message, very easy to use. 
See simple chat server example for usage. Everything you need is contained within the two class files TCPClient.cs and TCPServer.cs in TCPUtility. A better documentation will be added soon, for now just check the examples.

##Usage

This shows the basics of how to use the TCP Utility, but check the application example to view in detail how to write and use your eventhandlers and how to enable encryption.

Basic server example:

List<TCPClient> clients = new List<TCPClient>(); //List to hold connected clients
TCPServer server = new TCPServer(1337); //Listen port 1337
server.ClientConnect += Server_ClientConnect; //Eventhandler (check example app for implementation)
server.Start(); //Start server

Basic client example:

TCPClient client = new TCPClient("localhost", 1337);
client.Connect();
client.MessageReceived += Client_MessageReceived; //Eventhandler (check example app for implementation)
client.Send("Hello governour!");

Licensed under MIT License.
Note: This is a very new project (ie. some ugly code and not very well documented nor tested)
