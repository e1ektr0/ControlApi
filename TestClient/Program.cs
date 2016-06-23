using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ControlApi;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(1000);
            var client = new TcpClient();
            client.Connect("127.0.0.1", 4520);
            var writer = new StreamWriter(client.GetStream());
            SmothZoom(writer);
            Console.ReadLine();
        }

        private static void SmothZoom(StreamWriter writer)
        {
            writer.WriteLine($"{(int)Commands.CtrDown}");
            writer.Flush();
            writer.WriteLine($"{(int)Commands.MiddleDown}");
            writer.Flush();
            for (var i = 0; i < 300; i++)
            {
                writer.WriteLine($"{(int)Commands.MouseMove},0,1");
                writer.Flush();
            }
            writer.WriteLine($"{(int)Commands.MiddleUp}");
            writer.Flush();
            writer.WriteLine($"{(int)Commands.CtrUp}");
            writer.Flush();
        }
    }
}
