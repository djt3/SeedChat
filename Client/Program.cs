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

            Client.BroadcastMessage(new Message { MessageType = (uint)MessageTypes.KeyExchange, Message_ = Messaging.publicKey, ToId = id, FromId = Client.Id.ToString() });

            while (true)
            {
                string message = Messaging.EncryptMessage(id, Console.ReadLine());

                Client.BroadcastMessage(new Message { MessageType = (uint)MessageTypes.Message, Message_ = message, ToId = id, FromId = Messaging.EncryptMessage(id, Client.Id.ToString()) });
                Console.WriteLine($"You: {message}");
            }

            server.ShutdownAsync().Wait();
        }
    }
}
