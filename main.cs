/*
--------------------------------------------------------------------
* Name:       Stan Turovsky
* Class:      CPSC 24500 -- Object-Oriented Programming
* Assignment: HW 4
* File:       main.cs
* Purpose:    Chatroom server in C#
*             -Shows usage of class constructors and destructors
*             -Needs to run before the chat client
--------------------------------------------------------------------
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChatServer
{
    public static class ByteExtension
    {
        public static string GetString(this byte[] arr, int k)
        { return Encoding.ASCII.GetString(arr, 0, k); }
    }
    public static class StringExtension
    {
        public static byte[] ToByteArray(this string str)
        { return Encoding.ASCII.GetBytes(str); }
    }
    class Chatroom
    {
        private Dictionary<Socket, string> ConnectionHandlers = new Dictionary<Socket, string>();
        int _connections;             // counts connections
        Socket _myChatSocket;         // current chat socket
        public Chatroom()             // class constructor
        {
            Console.WriteLine("Chat room created!");
        }

        ~Chatroom()                   // class destructor
        {
            Console.WriteLine("Chat room closed!");
        }

        public void Message(Socket s, string msg, string usr)
        {
            string newMessage = String.Format($"{usr}: {msg}");
            Console.WriteLine(newMessage);
            try
            {
                foreach (Socket sock in ConnectionHandlers.Keys)
                {
                    if (sock != s)
                        sock.Send(newMessage.ToByteArray());
                }
            }
            catch (SocketException)
            {
                Console.WriteLine($"Message: {usr} disconnected!");
                ConnectionHandlers.Remove(_myChatSocket);
                _connections -= 1;                           // reduce connection count
            }
            catch (Exception e) // Exception e
            {
                Console.WriteLine($"Exception: {e.Message}");
            }
        }

        public void PingConnections(Socket s, string msg)
        {
            try
            {
                //Console.WriteLine("Pinging!");
                foreach (KeyValuePair<Socket, string> client in ConnectionHandlers)
                {
                    client.Key.Send(StringExtension.ToByteArray(""));
                    //if (client.Key != MyChatSocket)
                    //{
                    //    client.Key.Send(StringExtension.ToByteArray($"Ping!"));
                    //}
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"PingConnections: {e.Message}");
            }
        }
        public void AddConnection(Socket s)
        {
            _connections += 1;
            _myChatSocket = s;
            Thread tRd = new Thread(ReadData);
            tRd.Start();
        }

        public void ReadData()      //object socket
        {
            _myChatSocket.Send(StringExtension.ToByteArray("Username?"));    // ask for user name
            byte[] buffer = new byte[1024];                                  // buffer byte array
            int k = _myChatSocket.Receive(buffer);                            
            string username = ByteExtension.GetString(buffer, k);            // read user name
            if (username.Length<1)
                username = DateTime.Now.Ticks.ToString();            // handles missing user name
            ConnectionHandlers.Add(_myChatSocket, username);         // add new user
            Console.WriteLine($"Connection accepted from {_myChatSocket.RemoteEndPoint}");

            while (true)
            {
                try
                {
                    k = _myChatSocket.Receive(buffer);
                    string msg = ByteExtension.GetString(buffer, k);
                    //SendGlobalMessage(MyChatSocket, msg);              // EventMessage (not used)

                    Message(_myChatSocket, msg, username);               // replaces code below

                    //foreach (KeyValuePair<Socket, string> otherClient in ConnectionHandlers)
                    //{
                    //    if (otherClient.Key != MyChatSocket)
                    //    {
                    //        string clientData = $"{username} - " + msg;
                    //        otherClient.Key.Send(StringExtension.ToByteArray(clientData));
                    //    }
                    //}
                    //Console.WriteLine("{0}: {1}", username, msg);
                }
                catch (SocketException)
                {
                    ConnectionHandlers.Remove(_myChatSocket);    // remove user from the connections
                    _connections -= 1;                           // reduce connection count
                    Console.WriteLine($"{username} disconnected! {_connections} connections remain(s)...");
                    break;
                }
                catch (Exception e) // Exception e
                {
                    Console.WriteLine($"ReadData exception: {e.Message}");   // not a socket exception!
                    break;
                }
            }
            //MyChatSocket.Close();
        }
        public delegate void EventMessage(Socket s, string msg);
        // public event EventMessage SendGlobalMessage;     // EventMessage (not used)
    }
    class Server : TcpListener
    {
        Socket _mySocket;

        public Server(): base(IPAddress.Loopback, 5000)
        {
            this.Start();                           // starts server
            Console.WriteLine("Server started!");

            Thread tHc = new Thread(HandleConnections);
            tHc.Start();                            // start connection handling thread
        }

        ~Server()
        {
            this.Stop();                            // stops server
            Console.WriteLine("Server stopped!");
        }

        public void HandleConnections()
        {
            Console.WriteLine("Waiting for a connection...");
            while (true)
            {
                _mySocket = AcceptSocket();
                OnSocketAccept?.Invoke(_mySocket); // client connection
            }
            // no return because of multi-threading
        }
        public delegate void EventConnection(Socket s);
        public event EventConnection OnSocketAccept;

    }
    class Program
    {
        static void Main()
        {
            Server svr = new Server();
            Chatroom room = new Chatroom();

            // Attempt to access non-static versions of svr and room -- failed
            //Program P = new Program();
            //P.svr.OnSocketAccept += P.room.AddConnection;

            svr.OnSocketAccept += room.AddConnection;

            // Attempt to use mainTimer in a thread -- failed
            //Thread tMT = new Thread(mainTimer);
            //tMT.Start();

            // below didn't work as expected
            //mainTimer();  

            while (true)
            {
                // Attempt to use SendGlobalMessage event -- failed
                //room.SendGlobalMessage += room.PingConnections;
                Thread.Sleep(100);
            }
            // no return because of multi-threading
        }
    }
}
