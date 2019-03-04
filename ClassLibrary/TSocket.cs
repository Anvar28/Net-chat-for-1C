using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClassLibrary
{
    public delegate void OnReceive(TSocket client, string str);
    public delegate void OnLog(string str);
    public delegate void OnAction(TSocket client);
    public delegate void OnError(TSocket client, Exception e);

    public enum commands
    {
        sendString = 1,
        sendFile = 2
    }

    public enum TStatusSocket
    {
        none,
        receiveStream
    }

    public abstract class TSocket
    {
        protected internal Socket _socket;
        protected internal TMemoryStream _msReceive;
        protected internal BinaryReader _readerReceive;
        protected internal byte[] _bufReceive;
        protected internal TStatusSocket _statusReceive;

        // Events

        public OnLog OnLog;
        public OnAction OnConnect;
        public OnAction OnDisconnect;
        public OnError OnError;
        public OnReceive OnReceive;

        public bool Connected
        {
            get
            {
                if (_socket == null)
                    return false;
                else
                    return _socket.Connected;

            }
        }

        // Constructor

        public TSocket()
        {
            _msReceive = new TMemoryStream();
            _readerReceive = new BinaryReader(_msReceive.ms);
            _bufReceive = new byte[1024];
        }

        // Events

        protected internal void _Log(string str)
        {
            OnLog?.Invoke(str);
        }

        protected internal void _OnDisconnect()
        {
            OnDisconnect?.Invoke(this);
        }

        protected internal void _OnConnect()
        {
            OnConnect?.Invoke(this);
        }

        protected internal void _OnError(Exception e)
        {
            OnError?.Invoke(this, e);
        }

        protected internal void _OnReceive(string str)
        {
            OnReceive?.Invoke(this, str);
        }

        // Disconnect

        public virtual void Disconnect()
        {
            if (_socket.Connected)
            {
                _socket.Shutdown(SocketShutdown.Send);
                _socket.Close();
            }
            _OnDisconnect();
        }

        // Send

        public virtual void SendString(string str)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ms);
            writer.Write((Int32)commands.sendString);
            byte[] _bufData = Encoding.Unicode.GetBytes(str);
            writer.Write((Int32)_bufData.Length);
            writer.Write(_bufData);
            Send(ms);
        }

        public virtual void Send(MemoryStream ms)
        {
            if (_socket.Connected)
                try
                {
                    _socket.Send(ms.ToArray());
                }
                catch (Exception e)
                {
                    _Log("Error send data");
                    _OnError(e);
                }
        }

        // Receive

        public void BeginReceive()
        {
            Array.Clear(_bufReceive, 0, _bufReceive.Length);
            try
            {
                _socket.BeginReceive(_bufReceive, 0, _bufReceive.Length, SocketFlags.None, RecieveCallBack, null);
            }
            catch (Exception e)
            {
                Disconnect();
                _OnError(e);
            }
        }

        protected internal virtual TStatusSocket ProcessingData()
        {
            const int lengthHead = 8;

            while (_msReceive.Length > 0)
            {
                _msReceive.Position = 0;

                // read command and length 

                if (_msReceive.Length < lengthHead)
                    return TStatusSocket.receiveStream;

                commands commandReceive = (commands)_readerReceive.ReadInt32();
                int lengthDataReceive = _readerReceive.ReadInt32();

                // not complyte receive

                if (_msReceive.Length < lengthDataReceive + lengthHead)
                    return TStatusSocket.receiveStream;

                // all received

                if (_msReceive.Length >= lengthDataReceive + lengthHead)
                {
                    _msReceive.Position = lengthHead;
                    switch (commandReceive)
                    {
                        case commands.sendString:
                            byte[] buf = new byte[lengthDataReceive];
                            _msReceive.ms.Read(buf, 0, lengthDataReceive);
                            string str = Encoding.Unicode.GetString(buf);
                            _Log("str " + str);

                            _OnReceive(str);
                            _msReceive.TruncateFromTop(lengthDataReceive + lengthHead);

                            break;
                        case commands.sendFile:

                            _OnError(new Exception("File receive not complit"));
                            _msReceive.Clear();

                            break;
                        default:

                            _msReceive.Clear();

                            break;
                    }
                }
            }
            return TStatusSocket.none;
        }

        protected internal void RecieveCallBack(IAsyncResult result)
        {
            int bufLen = 0;
            try
            {
                // Receive data
                bufLen = _socket.EndReceive(result);
            }
            catch (Exception e)
            {
                _Log("Connect close error: " + e.Message);
                _OnError(e);
                Disconnect();
                return;
            }

            if (bufLen == 0)
            {
                _Log("Connect close bufLen = 0");
                Disconnect();
                return;
            }

            // copy data to memory
            _msReceive.Position = _msReceive.Length;
            _msReceive.Write(_bufReceive, 0, bufLen);

            // processing data
            _Log("Receive data length: " + bufLen.ToString() + " save to _MS, length _MS: " + _msReceive.Length.ToString());
            _statusReceive = ProcessingData();

            // receive continue
            BeginReceive();
        }

    }

    public class TClientSocket: TSocket
    {
        // Connect

        public void Connect(string ip, int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // Create new connection

            try
            {
                _socket.BeginConnect(IPAddress.Parse(ip), port, ConnectCallBack, null);
            }
            catch (Exception)
            {
                try
                {
                    IPHostEntry a = Dns.GetHostEntry(ip);
                    if (a.AddressList.Count() > 0)
                        try
                        {
                            _socket.BeginConnect(a.AddressList[0], port, ConnectCallBack, null);
                        }
                        catch (Exception e1)
                        {
                            _OnError(e1);
                        }
                }
                catch (Exception e2)
                {
                    _OnError(e2);
                }
                    
            }

        }

        private void ConnectCallBack(IAsyncResult result)
        {
            try
            {
                _socket.EndConnect(result);
            }
            catch (Exception e)
            {
                _Log("Error connect " + e.Message);
                _OnError(e);
            }

            if (_socket.Connected)
            {
                _Log("Connect to " + _socket.RemoteEndPoint.ToString());
                _OnConnect();
                _statusReceive = TStatusSocket.none;
                BeginReceive();
            }
        }
    }

    public class TServerClientSocket: TSocket
    {
        public string _name;

        public TServerClientSocket(Socket socket)
        {
            _socket = socket;
        }
    }
}
