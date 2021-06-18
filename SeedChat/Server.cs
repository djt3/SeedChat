using Grpc.Core;
using SeedChat;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeedChatClient
{
    class ChatServerImpl : ChatServer.ChatServerBase
    {
        Client client;

        public ChatServerImpl(Client client)
        {
            this.client = client;
        }

        public override Task<CodedResponse> Ping(EmptyMessage message, ServerCallContext context)
        {
            Console.WriteLine($"recieved ping from {context.Peer}");

            return Task.FromResult(new CodedResponse { Code = 1 });
        }

        public override Task<CodedResponse> RequestStore(StoreRequest request, ServerCallContext context)
        {
            Console.WriteLine($"recieved store request from {request.NodeAddress}");

            if (!client.ContainsNodeAddress(request.NodeAddress))
            {
                client.nodes.Add(new Node(request.NodeAddress));
            }

            return Task.FromResult(new CodedResponse { Code = 1 });
        }

        public override Task<CodedResponse> RequestSeed(SeedRequest request, ServerCallContext context)
        {
            if (client.Id == request.ClientId)
                return Task.FromResult(new CodedResponse { Code = 1 });

            Console.WriteLine($"recieved seed request for {request.ClientId}");

            client.AddNode(request.NodeAddress);

            return Task.FromResult(new CodedResponse { Code = 1 });
        }

        public override Task<CodedResponse> SendMessage(Message message, ServerCallContext context)
        {
            if (message.ToId == this.client.Id)
            {
                if (message.MessageType == (uint)MessageTypes.Message)
                {
                    Console.WriteLine($"Them: {Messaging.DecryptMessage(message.FromId, message.Message_)}");
                    return Task.FromResult(new CodedResponse { Code = 1 });
                }

                else if (message.MessageType == (uint)MessageTypes.KeyExchange)
                {
                    Console.WriteLine($"Recieved key exchange from {message.FromId}");

                    Messaging.AddPublicKey(UInt64.Parse(message.FromId), message.Message_);
                }
            }

            if (!routeTable.ContainsKey(message.ToId))
                return Task.FromResult(new CodedResponse { Code = 0 });

            Console.WriteLine($"recieved message {message.Message_} for {message.ToId}");

            foreach (Node node in routeTable[message.ToId])
            {
                node.Client.SendMessageAsync(message);
            }

            return Task.FromResult(new CodedResponse { Code = 1 });
        }

        public override async Task GetNodes(EmptyMessage message, IServerStreamWriter<NodeResponse> response,
            ServerCallContext context)
        {
            foreach (Node node in Client.nodes)
            {
                await response.WriteAsync(new NodeResponse { Address = node.Address });
            }
        }
    }
}
