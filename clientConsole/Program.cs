using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using client;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace clientConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Socket _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(IPAddress.Parse("127.0.0.1"), 5050);
            MemoryStream _ms = new MemoryStream();
            BinaryWriter _bw = new BinaryWriter(_ms);

            string data = Console.ReadLine();

            while (data != "q") {
                _ms.Position = 0;
                _bw.Write((Int32)1);                                 // Command 
                byte[] _bufData = Encoding.Unicode.GetBytes(data);
                _bw.Write((Int32)_bufData.Length);                   // Length data
                _bw.Write(_bufData);                                     // Data
                try
                {
                    _socket.Send(_ms.ToArray());
                }
                catch (Exception)
                {
                    break;
                }
                data = Console.ReadLine();
            }
        }
    }
}
