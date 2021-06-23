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

namespace SeedChat
{
    public class Node
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

    public class Client
    {
        public int Port = 0;
        public UInt64 Id = 0;
        public ConcurrentDictionary<UInt64, List<Node>> RouteTable = new();

        Server server;
        List<Node> nodes = new();

        Logger logger = new();
        public Messaging Messaging = new();

        public Client()
        {
            this.Port = 4242;

            byte[] idArray = new byte[64];

            Random random = new Random();

            random.NextBytes(idArray);

            this.Id = BitConverter.ToUInt64(idArray);
        }

        public Client(UInt64 id, int port)
        {
            this.Id = id;
            this.Port = port;
        }

        public bool BroadcastMessage(Message message)
        {
            bool ret = false;

            foreach (Node node in this.nodes)
            {
                bool result = node.Client.SendMessage(message).Code == 1;

                if (!ret && result)
                {
                    ret = true;
                }
            }

            return ret;
        }

        public bool SendMessageToId(UInt64 toId, string message)
        {
            message = Messaging.EncryptMessage(toId, message);

            string id = Messaging.EncryptId(toId, this.Id.ToString());

            return BroadcastMessage(new Message { Message_ = message, ToId = toId, FromId = id, MessageType = (uint)MessageTypes.Message });
        }

        public bool SendKeyToId(UInt64 toId)
        {
            return BroadcastMessage(new Message { Message_ = Messaging.PublicKey, FromId = this.Id.ToString(), ToId = toId, MessageType = (uint)MessageTypes.KeyExchange });
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
                return false;

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
            if (!this.RouteTable.ContainsKey(id))
            {
                this.RouteTable[id] = new List<Node>();
            }

            this.RouteTable[id].Add(new Node(address));
        }

        public void Reseed(SeedRequest request)
        {
            foreach (Node node in this.nodes)
            {
                node.Client.RequestSeed(request);
            }
        }

        public void Seed()
        {
            foreach (Node node in this.nodes)
            {
                node.RequestSeed(this.Id, this.Port);
            }
        }

        public delegate void RecieveMessage(string message, UInt64 fromId);
        public event RecieveMessage RecieveMessageEvent;

        public bool OnMessageRecieved(Message message)
        {
            if (message.ToId == this.Id)
            {
                if (message.MessageType == (uint)MessageTypes.Message)
                {
                    UInt64 fromId = Messaging.DecryptId(message.FromId);

                    if (fromId == 0)
                    {
                        logger.LogError("Invalid sender id");
                    }

                    string decrypted = Messaging.DecryptMessage(fromId, message.Message_);

                    this.logger.Log($"Them: {decrypted}");

                    if (RecieveMessageEvent != null)
                    {
                        RecieveMessageEvent(decrypted, fromId);
                    }

                    return true;
                }

                else if (message.MessageType == (uint)MessageTypes.KeyExchange)
                {
                    this.logger.Log($"Recieved key exchange from {message.FromId}");

                    try
                    {
                        this.Messaging.AddPublicKey(UInt64.Parse(message.FromId), message.Message_);
                    }

                    catch (Exception)
                    {
                        this.logger.Log("Invalid sender id for key exchange");
                    }
                }
            }

            if (!RouteTable.ContainsKey(message.ToId))
                return false;

            this.logger.Log($"recieved message {message.Message_} for {message.ToId}");

            foreach (Node node in RouteTable[message.ToId])
            {
                node.Client.SendMessage(message);
            }

            return true;
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

                    this.server.Start();

                    break;
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

            this.Messaging.Initialize();

            return true;
        }
    }
}
