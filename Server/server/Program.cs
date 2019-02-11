using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using logers;

namespace server
{
    delegate void SendStringAll(string str, TClientSocket excludeSocket = null);

    enum TStatusSocket
    {
        none,
        receiveStream
    }

    class TClientSocket
    {
        public Socket _socket;
        private MemoryStream _msReceive;
        private BinaryReader _readerReceive;
        private byte[] _bufReceive;
        private server.commands _commandReceive;
        private TStatusSocket _statusReceive;
        private int _lengthDataReceive;

        private BinaryWriter _writer;
        public string name;
        private SendStringAll sendStringAll;
        private ILoger _loger;

        public TClientSocket(Socket lsocket, SendStringAll lSendStringAll, ILoger loger)
        {
            _loger = loger;
            _socket = lsocket;
            _statusReceive = TStatusSocket.none;

            _bufReceive = new byte[10];
            _msReceive = new MemoryStream();
            _readerReceive = new BinaryReader(_msReceive);
            _writer = new BinaryWriter(_msReceive);

            sendStringAll = lSendStringAll;

            BeginReceive();
        }

        private void ClearBuff()
        {
            Array.Clear(_msReceive.GetBuffer(), 0, _msReceive.GetBuffer().Length);
            _msReceive.Position = 0;
        }

        public void SendString(string str)
        {
            ClearBuff();
            _writer.Write((Int32)server.commands.sendString);
            _writer.Write(str);
            Send();
        }

        private void Send()
        {
            _socket.Send(_msReceive.ToArray());
        }

        public void Close()
        {
            //socket.Disconnect(true);
        }

        private void BeginReceive()
        {
            Array.Clear(_bufReceive, 0, _bufReceive.Length);
            try
            {
                _socket.BeginReceive(_bufReceive, 0, _bufReceive.Length, SocketFlags.None, RecieveCallBack, null);
            }
            catch (Exception)
            {
                _loger.Write("Connect close");
            }
        }

        private void TruncateMemoryStreamFromTop(MemoryStream ms, int numberOfBytesToRemove)
        {
            byte[] buf = ms.GetBuffer();
            Buffer.BlockCopy(buf, numberOfBytesToRemove, buf, 0, (int)ms.Length - numberOfBytesToRemove);
            ms.SetLength(ms.Length - numberOfBytesToRemove);
        }

        public void RecieveCallBack(IAsyncResult result)
        {
            int bufLen = 0;
            try
            {
                // Receive data
                bufLen = _socket.EndReceive(result);
            }
            catch (Exception)
            {
                _loger.Write("Connect close");
                return;
            }            

            // copy data to memory
            _msReceive.Write(_bufReceive, 0, bufLen);

            // processing data
            _loger.Write("Receive data length: " + bufLen.ToString() + " save to _MS, length _MS: " + _msReceive.Length.ToString());

            // analys head 

            if (_statusReceive == TStatusSocket.none)
            {
                _msReceive.Position = 0;

                // read command and length 
                _commandReceive = (server.commands)_readerReceive.ReadInt32();
                _lengthDataReceive = _readerReceive.ReadInt32();

                TruncateMemoryStreamFromTop(_msReceive, 8); // this is (4 byte command + 4 byte length)

                if (_msReceive.Length < _lengthDataReceive)
                {
                    _statusReceive = TStatusSocket.receiveStream;
                    _msReceive.Position = _msReceive.Length;
                }                
            }
            
            // if not complyte receive

            else
            {
                if (_msReceive.Length >= _lengthDataReceive)
                {
                    _statusReceive = TStatusSocket.none;
                }
            }

            if (_statusReceive == TStatusSocket.none) // All received
            {
                switch (_commandReceive)
                {
                    case commands.sendString:

                        _msReceive.Position = 0;
                        string str = Encoding.Unicode.GetString(_readerReceive.ReadBytes(_lengthDataReceive));
                        _loger.Write("str " + str);

                        _loger.Write("Length _MS before " + _msReceive.Length.ToString());
                        TruncateMemoryStreamFromTop(_msReceive, (int)_msReceive.Position);
                        _loger.Write("Length _MS after " + _msReceive.Length.ToString());

                        break;
                    case commands.sendFile:

                        new Exception("File receive not complit");

                        break;
                    default:
                        break;
                }

                _msReceive.Position = 0;
            }

            // receive continue
            BeginReceive();
        }
    }

    class TServer
    {
        private ILoger loger;
        private Socket serverSocket;
        private List<TClientSocket> ClientList;

        public TServer(int port, ILoger lLoger)
        {
            loger = lLoger;

            ClientList = new List<TClientSocket>();

            serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(0);
        }

        public void Start()
        {
            BeginAccept();
        }

        private void BeginAccept()
        {
            serverSocket.BeginAccept(AcceptCallBack, null);
        }
        
        private void AcceptCallBack(IAsyncResult ar)
        {
            loger.Write("Connect new client");
            TClientSocket clientSocket = new TClientSocket(serverSocket.EndAccept(ar), this.SendStringAll, loger);
            ClientList.Add(clientSocket);
            BeginAccept();
        }

        public void SendStringAll(string str, TClientSocket excludeSocket = null)
        {
            bool flag;
            foreach (TClientSocket item in ClientList)
            {
                flag = true;
                if (item != null)
                {
                    if (item != excludeSocket)
                    {
                        flag = false;
                    }
                }
                if (flag) {
                    item.SendString(str);
                }
            }
        }
    }

    class Program
    {
        const int port = 5050; // порт для прослушивания подключений

        static void Main(string[] args)
        {
            logConsol loger = new logConsol();
            loger.Write("Start");
            TServer server = new TServer(port, loger);
            server.Start();
            Console.ReadLine();
        }
    }
}
