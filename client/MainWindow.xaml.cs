using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace client
{

    delegate void OnReceive(string str);
    delegate void Log(string str);
    delegate void OnAction(TClient client);

    enum TStatusSocket
    {
        none,
        receiveStream
    }

    class TClient
    {
        private Socket _socket;
        private MemoryStream _msReceive;
        private BinaryReader _readerReceive;
        private byte[] _bufReceive;
        private TStatusSocket _statusReceive;
        private server.commands _commandReceive;
        private int _lengthDataReceive;

        // 

        public Log log;

        // Events

        public OnAction OnConnect;
        public OnAction OnDisconnect;
        public OnReceive OnReceive;

        public bool Connected
        {
            get {
                if (_socket == null)
                    return false;
                else
                    return _socket.Connected;

            }
        }

        // Constructor

        public TClient()
        {            
            _msReceive = new MemoryStream();
            _readerReceive = new BinaryReader(_msReceive);
            _bufReceive = new byte[1024];
        }

        // Events

        private void _Log(string str)
        {
            log(str);
        }

        private void _OnDisconnect()
        {
            OnDisconnect(this);
        }

        private void _OnConnect()
        {
            OnConnect(this);
        }

        private void _OnReceive(string str)
        {
            OnReceive(str);
        }

        // Connect

        public void Connect(string ip, int port)
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp); // Create new connection
            _socket.BeginConnect(IPAddress.Parse(ip), port, ConnectCallBack, null);
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
            }

            if (_socket.Connected)
            {
                _OnConnect();
                _Log("Connect to " + _socket.RemoteEndPoint.ToString());
                _statusReceive = TStatusSocket.none;
                BeginReceive();
            }
        }
    
        // Disconnect

        public void Disconnect()
        {
            if (_socket.Connected)
            {
                _socket.Shutdown(SocketShutdown.Send);
                _socket.Close();
            }            
            _OnDisconnect();
        }

        // Send

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
            if (_socket.Connected)
                try
                {
                    _socket.Send(ms.ToArray());
                }
                catch (Exception)
                {
                    _Log("Error send data");
                }
        }

        // Receive

        private void BeginReceive()
        {
            Array.Clear(_bufReceive, 0, _bufReceive.Length);
            try
            {
                _socket.BeginReceive(_bufReceive, 0, _bufReceive.Length, SocketFlags.None, RecieveCallBack, null);
            }
            catch (Exception)
            {
                Disconnect();
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
                Disconnect();
                _Log("Connect close");
                return;
            }

            if (bufLen == 0)
            {
                Disconnect();
                _Log("Connect close");
                return;
            }

            // copy data to memory
            _msReceive.Write(_bufReceive, 0, bufLen);

            // processing data
            //_loger.Write("Receive data length: " + bufLen.ToString() + " save to _MS, length _MS: " + _msReceive.Length.ToString());

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
                _msReceive.Position = 0;
                switch (_commandReceive)
                {
                    case server.commands.sendString:
                        string str = Encoding.Unicode.GetString(_msReceive.ToArray());
                        _Log("str " + str);

                        TruncateMemoryStreamFromTop(_msReceive, (int)_msReceive.Position);
                        _OnReceive(str);

                        break;
                    case server.commands.sendFile:

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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TClient client;

        public MainWindow()
        {
            InitializeComponent();

            client = new TClient();
            client.OnConnect = OnConnect;
            client.OnDisconnect = OnDisconnect;
            client.OnReceive = OnReceive;
            client.log = new Log(log);
        }

        private void OnConnect(TClient client)
        {
            log("Соединение установлено");
            setStatusControl();
        }

        private void OnDisconnect(TClient client)
        {
            log("Связь потеряна");
            setStatusControl();
        }

        private void OnReceive(string str)
        {
            log(str);
        }

        private void log(string str)
        {
            // Update data on the form
            this.Dispatcher.Invoke(
                delegate 
                {
                    lbLog.Items.Insert(0, DateTime.Now.ToString() + "\t" + str);
                }
            );
        }

        private void sendTextAndClear()
        {
            client.SendString(edtText.Text);
            edtText.Text = "";
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {

            if (!client.Connected)
            {
                log("Попытка подключение к " + edtIP.Text + ":" + edtPort.Text);
                int port = 20500;
                int.TryParse(edtPort.Text, out port);
                client.Connect(edtIP.Text, port);
            }
        }

        private void setStatusControl()
        {
            // Update data on the form
            this.Dispatcher.Invoke(
                delegate
                {
                    btnConnect.IsEnabled = !client.Connected;
                    btnDisconnect.IsEnabled = !btnConnect.IsEnabled;
                    btnSend.IsEnabled = btnDisconnect.IsEnabled;
                }
            );
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (client.Connected)
            {
                client.Disconnect();
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            sendTextAndClear();
        }

        private void edtText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                sendTextAndClear();
            }
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            client.Disconnect();
        }
    }
}
