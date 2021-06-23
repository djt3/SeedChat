using Grpc.Core;
using SeedChat;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeedChat
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

            client.AddNode(request.NodeAddress);

            return Task.FromResult(new CodedResponse { Code = 1 });
        }

        public override Task<CodedResponse> RequestSeed(SeedRequest request, ServerCallContext context)
        {
            if (client.Id == request.ClientId)
                return Task.FromResult(new CodedResponse { Code = 1 });

            Console.WriteLine($"recieved seed request for {request.ClientId}");

            client.AddRoute(request.ClientId, request.NodeAddress);

            return Task.FromResult(new CodedResponse { Code = 1 });
        }

        public override Task<CodedResponse> SendMessage(Message message, ServerCallContext context)
        {
            this.client.OnMessageRecieved(message);

            return Task.FromResult(new CodedResponse { Code = 1 });
        }

        public override async Task GetNodes(EmptyMessage message, IServerStreamWriter<NodeResponse> response,
            ServerCallContext context)
        {
            //foreach (Node node in this..nodes)
            //{
            //await response.WriteAsync(new NodeResponse { Address = node.Address });
            //}
        }
    }
}
