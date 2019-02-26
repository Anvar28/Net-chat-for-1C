using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using logers;
using server.Classes;

namespace server
{
    delegate void SendStringAll(string str, TServerClientSocket excludeSocket = null);

    class TServer
    {
        private ILoger loger;
        private Socket serverSocket;
        private List<TServerClientSocket> ClientList;

        public TServer(int port, ILoger lLoger)
        {
            loger = lLoger;

            ClientList = new List<TServerClientSocket>();

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            serverSocket.Listen(0);
        }

        public void Start()
        {
            loger.Write("Start server");
            BeginAccept();
        }

        public void Log(string str)
        {
            loger.Write(str);
        }

        private void BeginAccept()
        {
            serverSocket.BeginAccept(AcceptCallBack, null);
        }

        private void DisconnectClient(TSocket client)
        {
            ClientList.Remove((TServerClientSocket)client);
            Log("Client disconnect. Count client "+ClientList.Count.ToString());
        }

        private void ReceiveClient(TSocket client, string str)
        {
            TServerClientSocket c = (TServerClientSocket)client;
            SendStringAll(str, c);
        }

        private void ErrorClient(TSocket client, Exception e)
        {
            Log(e.Message);
        }

        private void AcceptCallBack(IAsyncResult ar)
        {
            loger.Write("Connect new client");
            TServerClientSocket clientSocket = new TServerClientSocket(serverSocket.EndAccept(ar));
            clientSocket.OnLog = Log;
            clientSocket.OnDisconnect = DisconnectClient;
            clientSocket.OnReceive = ReceiveClient;
            clientSocket.OnError = ErrorClient;
            clientSocket.BeginReceive();
            ClientList.Add(clientSocket);
            BeginAccept();
        }

        public void SendStringAll(string str, TServerClientSocket excludeSocket = null)
        {
            bool flag;
            foreach (TServerClientSocket item in ClientList)
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
