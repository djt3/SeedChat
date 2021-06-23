using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeedChat
{
    class Logger
    {
        public void Log(string message)
        {
            Console.Write(message);
        }

        public void LogError(string message)
        {
            Console.Write("error: " + message);
        }
    }
}
