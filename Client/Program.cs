using Grpc.Core;
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

            if (Client.Initialize())
            {
                Console.WriteLine("initialized client");
            }

            else
            {
                Console.WriteLine("failed to initialized client");
            }

            Console.ReadLine();

            server.ShutdownAsync().Wait();
        }
    }
}
