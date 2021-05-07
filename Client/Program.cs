﻿using Grpc.Core;
using SeedChat;
using System;
using System.Diagnostics;

namespace SeedChatClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = null;

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    server = new Server
                    {
                        Services = { ChatServer.BindService(new ChatServerImpl()) },
                        Ports = { new ServerPort("localhost", Client.port, ServerCredentials.Insecure) },
                    };

                    server.Start();

                    Console.WriteLine($"server started on localhost:{Client.port}");

                    break;
                }

                catch (Exception)
                {
                    Console.WriteLine($"could not start on port {Client.port}, tryin again on {Client.port + 1}");

                    Client.port++;

                    if (i == 4)
                    {
                        Console.WriteLine("error! could not start server!");
                        Console.ReadLine();

                        return;
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Cyan;

            Messaging.Initialize();

            if (Client.Initialize())
            {
                Console.WriteLine("initialized client");
            }

            else
            {
                Console.WriteLine("failed to initialized client");
            }

            Console.WriteLine("enter id to send to:");
            UInt64 id = UInt64.Parse(Console.ReadLine());

            while(true)
            {
                string message = Console.ReadLine();

                foreach (Node node in Client.nodes) 
                {
                    node.client.SendMessage(new Message { MessageType = 1, Message_ = "hi", ToId = id });
                }
            }

            server.ShutdownAsync().Wait();
        }
    }
}
