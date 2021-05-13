/*
-----------------------------------------------------------------------------
* Name:       Stan Turovsky
* Class:      CPSC 24500 -- Object-Oriented Programming
* Assignment: HW 4
* File:       main.cs
* Purpose:    Chatroom client in C#
*             -Shows usage of multi-threading and a client/server environment
*             -Needs to run after the chat server (chatServer.exe)
-----------------------------------------------------------------------------
*/

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ChatClient
{
    class Program
    {
        static void ReadData(object socket)
        {
            TcpClient client = (TcpClient)socket;
            while (true)
            {
                byte[] buffer = new byte[1024];
                int k = client.GetStream().Read(buffer, 0, buffer.Length);
                //Console.WriteLine("Received from server {0}", Encoding.ASCII.GetString(buffer, 0, k));
                Console.WriteLine("{0}", Encoding.ASCII.GetString(buffer, 0, k));
            }
            // no return because of multi-threading
        }
        static void Main()
        {
            TcpClient client = new TcpClient();

            try
            {
                client.Connect(IPAddress.Loopback, 5000);
            }
            catch (SocketException)
            {
                Console.WriteLine("Please run the chat server first!");
                Console.ReadKey();
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("Regular exception: {0}", e);
                Console.ReadKey();
                return;
            }

            Thread t = new Thread(ReadData);
            t.Start(client);
            while (true)
            {
                Console.WriteLine("Please input a string to send");
                string input = Console.ReadLine();
                byte[] msg = Encoding.ASCII.GetBytes(input ?? string.Empty);

                client.GetStream().Write(msg, 0, msg.Length);

            }
            // no return because of multi-threading
        }
    }
}