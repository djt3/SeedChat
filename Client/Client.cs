using Grpc.Core;
using SeedChat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SeedChatClient
{
    class Node
    {
        public Node(string address)
        {
            this.address = address;

            Channel channel = new Channel(address, ChannelCredentials.Insecure);

            this.client = new ChatServer.ChatServerClient(channel);
        }

        public void RequestSeed()
        {
            this.client.RequestSeed(new SeedRequest { Bounces = 0, ClientId = Client.Id, NodeAddress = "localhost:" + Client.port });
        }

        public string address;
        public ChatServer.ChatServerClient client;
    }

    static class Client
    {
        public static UInt64 Id;
        public static int port = 4242;
        public static List<Node> nodes = new List<Node>();

        public static void BroadcastMessage(Message message)
        {
            foreach (Node node in nodes)
            {
                node.client.SendMessage(message);
            }
        }

        public static Node GetNodeWithAddress(string address)
        {
            foreach (Node node in nodes)
            {
                if (node.address == address)
                {
                    return node;
                }
            }

            return null;
        }

        public static bool ContainsNodeAddress(string address)
        {
            foreach (Node node in nodes)
            {
                if (node.address == address)
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task GrowNodeList()
        {
            foreach (Node node in nodes)
            {
                var call = node.client.GetNodes(new EmptyMessage());

                while (await call.ResponseStream.MoveNext())
                {
                    string address = call.ResponseStream.Current.Address;

                    if (ContainsNodeAddress(address) || address == "localhost:" + Client.port)
                        continue;

                    //nodes.Add(new Node(address));
                }
            }
        }

        public static bool Initialize()
        {
            byte[] idArray = new byte[64];

            Random random = new Random();

            random.NextBytes(idArray);

            Id = BitConverter.ToUInt64(idArray);

            Console.WriteLine($"client id {Id}");

            while (nodes.Count < 1)
            {
                Console.WriteLine("enter a node address to enter the network:");

                string address = Console.ReadLine();

                if (ContainsNodeAddress(address))
                    continue;

                Node node = new Node(address);

                try
                {
                    var stopwatch = new Stopwatch();

                    node.client.Ping(new EmptyMessage());

                    Console.WriteLine($"pinged node {address} in {stopwatch.ElapsedMilliseconds}ms");

                    nodes.Add(node);
                }

                catch (Exception)
                {
                    return false;
                }
            }

            //GrowNodeList().Wait();

            nodes[0].RequestSeed();

            return true;
        }
    }
}
