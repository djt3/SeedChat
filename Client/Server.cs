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
        public static ConcurrentDictionary<UInt64, List<Node>> routeTable = new ConcurrentDictionary<UInt64, List<Node>>();


        public override Task<CodedResponse> Ping(EmptyMessage message, ServerCallContext context)
        {
            Console.WriteLine($"recieved ping from {context.Peer}");

            return Task.FromResult(new CodedResponse { Code = 1 });
        }

        public override Task<CodedResponse> RequestStore(StoreRequest request, ServerCallContext context)
        {
            Console.WriteLine($"recieved store request from {request.NodeAddress}");

            if (!Client.ContainsNodeAddress(request.NodeAddress))
            {
                Client.nodes.Add(new Node(request.NodeAddress));
            }

            return Task.FromResult(new CodedResponse { Code = 1 });
        }

        public override Task<CodedResponse> RequestSeed(SeedRequest request, ServerCallContext context)
        {
            Console.WriteLine($"recieved seed request for {request.ClientId}");

            if (request.Bounces++ < 3)
            {
                request.NodeAddress = "localhost:" + Client.port;

                foreach (Node node in Client.nodes)
                {
                    node.client.RequestSeed(request);
                }
            }

            if (!Client.ContainsNodeAddress(request.NodeAddress))
            {
                Client.nodes.Add(new Node(request.NodeAddress));
            }

            routeTable[request.ClientId].Add(Client.GetNodeWithAddress(request.NodeAddress));

            return Task.FromResult(new CodedResponse { Code = 1 });
        }

        public override async Task GetNodes(EmptyMessage message, IServerStreamWriter<NodeResponse> response,
            ServerCallContext context)
        {
            foreach (Node node in Client.nodes)
            {
                await response.WriteAsync(new NodeResponse { Address = node.address });
            }
        }
    }
}
