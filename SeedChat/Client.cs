using Grpc.Core;
using SeedChat;
using System;
using System.Collections.Concurrent;
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
        public string Address;
        public ChatServer.ChatServerClient Client;

        public Node(string address)
        {
            this.Address = address;

            Channel channel = new Channel(address, ChannelCredentials.Insecure);

            this.Client = new ChatServer.ChatServerClient(channel);
        }

        public void RequestSeed(UInt64 clientId, int clientPort)
        {
            this.Client.RequestSeed(new SeedRequest { Bounces = 0, ClientId = clientId, NodeAddress = "localhost:" + clientPort });
        }
    }

    class Client
    {
        public UInt64 Id;
        public int Port = 4242;

        Server server;
        List<Node> nodes = new();
        Messaging messagng = new();
        ConcurrentDictionary<UInt64, List<Node>> routeTable = new();

        public void BroadcastMessage(Message message)
        {
            foreach (Node node in this.nodes)
            {
                node.Client.SendMessage(message);
            }
        }

        public Node GetNodeWithAddress(string address)
        {
            foreach (Node node in this.nodes)
            {
                if (node.Address == address)
                {
                    return node;
                }
            }

            return null;
        }

        public bool ContainsNodeAddress(string address)
        {
            foreach (Node node in this.nodes)
            {
                if (node.Address == address)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task GrowNodeList()
        {
            foreach (Node node in nodes)
            {
                var call = node.Client.GetNodes(new EmptyMessage());

                while (await call.ResponseStream.MoveNext())
                {
                    string address = call.ResponseStream.Current.Address;

                    if (ContainsNodeAddress(address) || address == "localhost:" + this.Port)
                        continue;

                    //nodes.Add(new Node(address));
                }
            }
        }

        public bool AddNode(string address)
        {
            if (this.ContainsNodeAddress(address))
                return;

            Node node = new(address);

            try
            {
                // make sure we can contact the node
                node.Client.Ping(new EmptyMessage());

                nodes.Add(node);

                return true;
            }

            catch (Exception)
            {
                return false;
            }
        }

        public void AddRoute(UInt64 id, string address)
        {
            if (!this.routeTable.ContainsKey(id))
            {
                this.routeTable[id] = new List<Node>();
            }

            this.routeTable[id].Add(this.GetNodeWithAddress(address));
        }

        public void Reseed(SeedRequest request)
        {
            foreach (Node node in this.nodes)
            {
                node.Client.RequestSeedAsync(request);
            }
        }

        public bool Initialize()
        {
            const int retries = 5;

            for (int i = 0; i < retries; i++)
            {
                try
                {
                    this.server = new Server
                    {
                        Services = { ChatServer.BindService(new ChatServerImpl(this)) },
                        Ports = { new ServerPort("localhost", this.Port, ServerCredentials.Insecure) },
                    };
                }

                catch (Exception)
                {
                    if (i == retries - 1)
                    {
                        return false;
                    }

                    this.Port++;
                }
            }

            byte[] idArray = new byte[64];

            Random random = new Random();

            random.NextBytes(idArray);

            this.Id = BitConverter.ToUInt64(idArray);

            this.messagng.Initialize();

            return true;
        }
    }
}
