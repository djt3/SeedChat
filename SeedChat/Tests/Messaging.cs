using SeedChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SeedChat.Tests
{
    public class MessagingTests
    {
        Messaging messaging1;
        Messaging messaging2;

        public MessagingTests()
        {
            messaging1 = new();
            messaging2 = new();

            messaging1.Initialize();
            messaging2.Initialize();

            messaging1.AddPublicKey(0, messaging2.PublicKey);
            messaging2.AddPublicKey(0, messaging1.PublicKey);
        }

        [Fact]
        public void KeyCheck()
        {
            Assert.NotEmpty(messaging1.Keys);
            Assert.NotEmpty(messaging2.Keys);
        }

        [Fact]
        public void MessageEncryption()
        {
            string message1 = messaging1.EncryptMessage(0, "message1");
            string message2 = messaging2.EncryptMessage(0, "message2");

            Assert.Contains("PGP", message1);
            Assert.Contains("PGP", message2);

            Assert.Equal("message1", messaging2.DecryptMessage(0, message1));
            Assert.Equal("message2", messaging1.DecryptMessage(0, message2));
        }

        [Fact]
        public void IdEncryption()
        {
            string id1 = messaging1.EncryptId(0, "100");
            string id2 = messaging2.EncryptId(0, "200");

            Assert.Contains("PGP", id1);
            Assert.Contains("PGP", id2);

            var test = messaging1.DecryptId(id2);

            Assert.True(messaging1.DecryptId(id2) == 200);
            Assert.True(messaging2.DecryptId(id1) == 100);
        }
    }
}
