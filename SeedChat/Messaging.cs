using PgpCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeedChatClient
{
    class Messaging
    {
        PGP pgp = new PGP();

        ConcurrentDictionary<UInt64, string> keys = new();

        string privateKey;
        public string PublicKey;

        string StreamToString(MemoryStream stream)
        {
            StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        public void Initialize()
        {
            MemoryStream publicKeyStream = new MemoryStream();
            MemoryStream privateKeyStream = new MemoryStream();

            this.pgp.GenerateKey(publicKeyStream, privateKeyStream);

            publicKeyStream.Position = 0;
            privateKeyStream.Position = 0;

            this.PublicKey = StreamToString(publicKeyStream);
            this.privateKey = StreamToString(privateKeyStream);
        }

        public string EncryptMessage(UInt64 id, string message)
        {
            return this.pgp.EncryptArmoredStringAndSign(message, keys[id], privateKey, "");
        }

        public string DecryptMessage(UInt64 id, string message)
        {
            return this.pgp.DecryptArmoredStringAndVerify(message, keys[id], privateKey, "");
        }

        public string EncryptId(UInt64 forId, string id)
        {
            return this.pgp.EncryptArmoredString(id, keys[forId]);
        }

        public string DecryptId(string id)
        {
            return this.pgp.DecryptArmoredString(id);
        }

        public void AddPublicKey(UInt64 id, string encKey)
        {
            if (this.keys.ContainsKey(id))
                return;

            this.keys.TryAdd(id, encKey);
        }
    }
}
