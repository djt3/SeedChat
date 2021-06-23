using SeedChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Priority;

namespace SeedChat.Tests
{
    public class ClientTestsFixture : IDisposable
    {
        public ClientTestsFixture()
        {
            clients = new();
            messages = new();

            for (int i = 0; i < 3; i++)
            {
                clients.Add(new((UInt64)i, 4242 + i));
                clients[i].Initialize();
            }

            for (int i = 0; i < 2; i++)
            {
                clients[i].AddNode("localhost:4244");
            }

            clients[0].RecieveMessageEvent += RecieveMessage;
        }

        public void RecieveMessage(string message, UInt64 fromId)
        {
            messages.Add(message);
        }

        public void Dispose()
        {

        }

        public List<Client> clients;
        public List<string> messages;
    }

    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class ClientTests : IClassFixture<ClientTestsFixture>
    {
        ClientTestsFixture fixture;

        public ClientTests(ClientTestsFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact, Priority(0)]
        public void TestPorts()
        {
            for (int i = 0; i < fixture.clients.Count; i++)
            {
                Assert.Equal(4242 + i, fixture.clients[i].Port);
            }
        }

        [Fact, Priority(1)]
        public void TestNodes()
        {
            Assert.True(fixture.clients[0].ContainsNodeAddress("localhost:4244"));
            Assert.True(fixture.clients[1].ContainsNodeAddress("localhost:4244"));
        }

        [Fact, Priority(2)]
        public void SeedTest()
        {
            fixture.clients[0].Seed();
            fixture.clients[1].Seed();

            Assert.True(fixture.clients[2].RouteTable.ContainsKey(fixture.clients[0].Id));
            Assert.True(fixture.clients[2].RouteTable.ContainsKey(fixture.clients[1].Id));
        }

        [Fact, Priority(3)]
        public void KeyExchangeTest()
        {
            Assert.True(fixture.clients[0].SendKeyToId(fixture.clients[1].Id));
            Assert.True(fixture.clients[1].SendKeyToId(fixture.clients[0].Id));

            Assert.Equal(fixture.clients[1].Messaging.PublicKey, fixture.clients[0].Messaging.Keys[fixture.clients[1].Id]);
            Assert.Equal(fixture.clients[0].Messaging.PublicKey, fixture.clients[1].Messaging.Keys[fixture.clients[0].Id]);
        }

        [Fact, Priority(4)]
        public void MessageTest()
        {
            Assert.True(fixture.clients[1].SendMessageToId(fixture.clients[0].Id, "hello"));

            Assert.NotEmpty(fixture.messages);
            Assert.Equal("hello", fixture.messages[0]);
        }
    }
}
