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
    static class Messaging
    {
        static PGP pgp = new PGP();

        public static ConcurrentDictionary<UInt64, string> keys = new ConcurrentDictionary<UInt64, string>();

        static string privateKey;
        public static string publicKey;

        static string StreamToString(MemoryStream stream)
        {
            StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        public static void Initialize()
        {
            MemoryStream publicKeyStream = new MemoryStream();
            MemoryStream privateKeyStream = new MemoryStream();

            pgp.GenerateKey(publicKeyStream, privateKeyStream);

            publicKeyStream.Position = 0;
            privateKeyStream.Position = 0;

            publicKey = StreamToString(publicKeyStream);
            privateKey = StreamToString(privateKeyStream);

            Console.WriteLine("generated encryption keys");
        }

        public static string EncryptMessage(UInt64 id, string message)
        {
            return pgp.EncryptArmoredStringAndSign(message, keys[id], privateKey, "");
        }

        public static string DecryptMessage(UInt64 id, string message)
        {
            return pgp.DecryptArmoredStringAndVerify(message, keys[id], privateKey, "");
        }

        public static void AddPublicKey(UInt64 id, string encKey)
        {
            if (keys.ContainsKey(id))
                return;

            keys.TryAdd(id, encKey);
        }
    }
}
