// Chatroom server
// Stan Turovsky

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;

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
        int Connections = 0;        // counts connections
        Socket MyChatSocket;        // current chat socket
        public Chatroom()
        {
            Console.WriteLine("Chat room created!");
        }

        ~Chatroom()
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
                ConnectionHandlers.Remove(MyChatSocket);
                Connections -= 1;                           // reduce connection count
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
            Connections += 1;
            MyChatSocket = s;
            Thread tRD = new Thread(ReadData);
            tRD.Start();
        }

        public void ReadData()      //object socket
        {
            MyChatSocket.Send(StringExtension.ToByteArray("Username?"));    // ask for user name
            byte[] buffer = new byte[1024];                                 // buffer byte array
            int k = MyChatSocket.Receive(buffer);                           // 
            string username = ByteExtension.GetString(buffer, k);           // read user name
            if (username.Length<1)
                username = DateTime.Now.Ticks.ToString();           // handles missing user name
            ConnectionHandlers.Add(MyChatSocket, username);         // add new user
            Console.WriteLine($"Connection accepted from {MyChatSocket.RemoteEndPoint}");

            while (true)
            {
                try
                {
                    k = MyChatSocket.Receive(buffer);
                    string msg = ByteExtension.GetString(buffer, k);

                    //SendGlobalMessage(MyChatSocket, msg);               // EventMessage (not used)

                    Message(MyChatSocket, msg, username);               // replaces code below

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
                    ConnectionHandlers.Remove(MyChatSocket);    // remove user from the connections
                    Connections -= 1;                           // reduce connection count
                    Console.WriteLine($"{username} disconnected! {Connections} connections remain(s)...");
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
        public event EventMessage SendGlobalMessage;
    }
    class Server : TcpListener
    {
        Socket MySocket;

        public Server(): base(IPAddress.Loopback, 5000)
        {
            this.Start();                           // starts server
            Console.WriteLine("Server started!");

            Thread tHC = new Thread(handleConnections);
            tHC.Start();                            // start connection handling thread
        }

        ~Server()
        {
            this.Stop();                            // stops server
            Console.WriteLine("Server stopped!");
        }

        public void handleConnections()
        {
            Console.WriteLine("Waiting for a connection...");
            while (true)
            {
                MySocket = AcceptSocket();
                OnSocketAccept(MySocket);                  // client connection
            }
        }
        public delegate void EventConnection(Socket s);
        public event EventConnection OnSocketAccept;

    }
    class Program
    {
        static void tick(object sender, ElapsedEventArgs e)
        {
            // not implemented
        }
        static void mainTimer()
        {
            System.Timers.Timer t = new System.Timers.Timer(2000);
            t.Elapsed += tick;
            t.Enabled = true;
            while (true)
            {
                Thread.Sleep(100);
            }
        }

        static void Main(string[] args)
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
        }
    }
}
