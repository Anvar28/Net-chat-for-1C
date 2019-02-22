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

    class TClientSocket
    {
        public Socket _socket;
        private MemoryStream _msReceive;
        private BinaryReader _readerReceive;
        private byte[] _bufReceive;
        private server.commands _commandReceive;
        private TStatusSocket _statusReceive;
        private int _lengthDataReceive;

        public string name;
        private SendStringAll sendStringAll;
        private ILoger _loger;

        public TClientSocket(Socket lsocket, SendStringAll lSendStringAll, ILoger loger)
        {
            _loger = loger;
            _socket = lsocket;
            _statusReceive = TStatusSocket.none;

            _bufReceive = new byte[1024];
            _msReceive = new MemoryStream();
            _readerReceive = new BinaryReader(_msReceive);

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
            MemoryStream ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms);
            writer.Write((Int32)server.commands.sendString);
            byte[] _bufData = Encoding.Unicode.GetBytes(str);
            writer.Write((Int32)_bufData.Length);
            writer.Write(_bufData);
            Send(ms);
        }

        private void Send(MemoryStream ms)
        {
            try
            {
                _socket.Send(ms.ToArray());
            }
            catch (Exception)
            {
                _loger.Write("Ошибка отправки данных");
            }
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
                Disconnect();
                return;
            }            

            if (bufLen == 0)
            {
                _loger.Write("Receive bufLen = 0");
                Disconnect();
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
                if (_lengthDataReceive < 0)
                {
                    _loger.Write("Receive _lengthDataReceive < 0");
                    Disconnect();
                    return;
                }

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
                _msReceive.Position = 0;
                switch (_commandReceive)
                {
                    case commands.sendString:
                        string str = Encoding.Unicode.GetString(_readerReceive.ReadBytes(_lengthDataReceive));
                        _loger.Write("str " + str);

                        //_loger.Write("Length _MS before " + _msReceive.Length.ToString());
                        TruncateMemoryStreamFromTop(_msReceive, (int)_msReceive.Position);
                        //_loger.Write("Length _MS after " + _msReceive.Length.ToString());

                        sendStringAll(str, this);

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

        public void Disconnect()
        {
            _socket.Disconnect(true);
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

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(0);
        }

        public void Start()
        {
            loger.Write("Start server");
            BeginAccept();
        }

        private void BeginAccept()
        {
            serverSocket.BeginAccept(AcceptCallBack, null);
        }
        
        private void AcceptCallBack(IAsyncResult ar)
        {
            loger.Write("Connect new client");
            TClientSocket clientSocket = new TClientSocket(serverSocket.EndAccept(ar), new SendStringAll(SendStringAll), loger);
            ClientList.Add(clientSocket);
            BeginAccept();
        }

        public void SendStringAll(string str, TClientSocket excludeSocket = null)
        {
            bool flag;
            foreach (TClientSocket item in ClientList)
            {
                // exclude socket
                flag = true;
                if (excludeSocket != null)
                    if (item == excludeSocket)
                        flag = false;

                if (flag) 
                    item.SendString(str);
                
            }
        }
    }

    class THttpServer
    {

        private Socket _httpServer;
        private ILoger loger;
        public SendStringAll SendStringAll;

        public THttpServer(int port, ILoger lLoger)
        {
            loger = lLoger;
            _httpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _httpServer.Bind(new IPEndPoint(IPAddress.Any, port));
            _httpServer.Listen(0);
        }

        public void Start()
        {
            loger.Write("Start http server");
            BeginAccept();
        }

        private void BeginAccept()
        {
            _httpServer.BeginAccept(AcceptCallBack, null);
        }

        private void AcceptCallBack(IAsyncResult ar)
        {
            loger.Write("Connect new http client");
            Socket clientSocket = (Socket)_httpServer.EndAccept(ar);
            HandleTheRequest(clientSocket);
            clientSocket.Close();
            BeginAccept();
        }

        private void HandleTheRequest(Socket clientSocket)
        {
            byte[] buffer = new byte[10240]; // 10 kb
            int receivedBCount = clientSocket.Receive(buffer);
            string strReceived = Encoding.UTF8.GetString(buffer, 0, receivedBCount);

            //loger.Write(strReceived);

            //parsing 

            string data = "";

            try
            {
                string httpMethod = strReceived.Substring(0, strReceived.IndexOf(" "));

                int start = strReceived.IndexOf("\r\n\r\n") + 4;
                int length = strReceived.Length - start;
                data = strReceived.Substring(start, length);

                loger.Write("Данные " + data);

                SendResponse(clientSocket, "OK");
            }
            catch (Exception e)
            {
                loger.Write("Error parse receive https server: " + e.Message + "\r\n" + strReceived);
            }

            SendStringAll(data);

        }

        private void SendResponse(Socket clientSocket, string strContent, string responseCode = "200 OK", string contentType = "text/html")
        {
            byte[] bContent = Encoding.UTF8.GetBytes(strContent);
            SendResponse(clientSocket, bContent, responseCode, contentType);
        }

        private void SendResponse(Socket clientSocket, byte[] bContent, string responseCode, string contentType)
        {
            try
            {
                byte[] bHeader = Encoding.ASCII.GetBytes(
                                    "HTTP/1.1 " + responseCode + "\r\n"
                                  + "Server: Atasoy Simple Web Server\r\n"
                                  + "Content-Length: " + bContent.Length.ToString() + "\r\n"
                                  + "Connection: close\r\n"
                                  + "Content-Type: " + contentType + "\r\n\r\n");
                clientSocket.Send(bHeader);
                clientSocket.Send(bContent);
                clientSocket.Close();
            }
            catch { }
        }
    }

    class Program
    {
        const int port = 5050; // порт для прослушивания подключений
        const int portHttp = 8080; // порт для http

        static void Main(string[] args)
        {
            logConsol loger = new logConsol();
            loger.Write("Start app");

            TServer server = new TServer(port, loger);
            server.Start();

            THttpServer httpServer = new THttpServer(portHttp, loger);
            httpServer.SendStringAll = server.SendStringAll;
            httpServer.Start();

            Console.ReadLine();
        }
    }
}
