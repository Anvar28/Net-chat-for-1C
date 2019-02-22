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
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace client
{

    delegate void OnReceive(string str);
    delegate void Log(string str);
    delegate void OnAction(TClient client);
    delegate void OnError(TClient client, Exception e);

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
        public OnError OnError;
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
            OnDisconnect?.Invoke(this);
        }

        private void _OnConnect()
        {
            OnConnect?.Invoke(this);
        }

        private void _OnError(Exception e)
        {
            OnError?.Invoke(this, e);
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
                catch (Exception e)
                {
                    _Log("Error send data");
                    _OnError(e);
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
            catch (Exception e)
            {
                Disconnect();
                _OnError(e);
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
            catch (Exception e)
            {
                _Log("Connect close");
                _OnError(e);
                Disconnect();
                return;
            }

            if (bufLen == 0)
            {
                _Log("Connect close");
                Disconnect();
                return;
            }

            // copy data to memory
            _msReceive.Write(_bufReceive, 0, bufLen);

            // processing data
            _Log("Receive data length: " + bufLen.ToString() + " save to _MS, length _MS: " + _msReceive.Length.ToString());

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

                        _OnReceive(str);

                        TruncateMemoryStreamFromTop(_msReceive, (int)_msReceive.Position);

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
        public const string strConnect = "Соединение установлено";
        public const string strDisconnect = "Соединение прекращено";
        public const int ReconectSecond = 10;

        private TClient _client;
        private WinForms.NotifyIcon _notifier = new WinForms.NotifyIcon();
        private DispatcherTimer _Timer;
        private bool Reconnect = true;

        public MainWindow()
        {
            InitializeComponent();

            _client = new TClient();
            _client.OnConnect = OnConnect;
            _client.OnDisconnect = OnDisconnect;
            _client.OnReceive = OnReceive;
            _client.OnError = OnError;
            _client.log = new Log(Log);

            this._notifier.MouseDown += new WinForms.MouseEventHandler(notifier_MouseDown);
            this._notifier.Icon = Properties.Resources.TrayIcon;
            this._notifier.Visible = true;
        }

        public void ShowBalloon(string text, int second = 3, string title="", WinForms.ToolTipIcon icon = WinForms.ToolTipIcon.Info)
        {
            if (title == "")
                title = "Звонок";
            _notifier.ShowBalloonTip(second * 1000, title, text, icon);
        }

        void notifier_MouseDown(object sender, WinForms.MouseEventArgs e)
        {
            if (e.Button == WinForms.MouseButtons.Right)
            {
                ContextMenu menu = (ContextMenu)this.FindResource("NotifierContextMenu");
                menu.IsOpen = true;            
            }
        }

        private void Menu_Open(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Open");
        }

        private void Menu_Close(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Close");
        }

        private void OnConnect(TClient client)
        {
            SetStatusControl();
            Log(strConnect);
            ShowBalloon(strConnect);
        }

        private void OnDisconnect(TClient client)
        {
            Log(strDisconnect);
            SetStatusControl();
            ShowBalloon(strDisconnect);
        }

        private void OnReceive(string str)
        {
            WriteMessageChat(str);
            ShowBalloon(str);
        }

        private void OnError(TClient client, Exception e)
        {
            Log(e.Message);
            if (!client.Connected && Reconnect)
            {
                // Запуск коннекта через N секунд
                StartReconnect();
            }
        }

        private void StartReconnect()
        {
            this.Dispatcher.Invoke(
                delegate
                {
                    Log("Попытка подключения через " + ReconectSecond.ToString()+" секунд.");
                    if (_Timer == null)
                    {
                        _Timer = new DispatcherTimer();
                    }

                    _Timer.Tick += new EventHandler(StartReconnectCallback);
                    _Timer.Interval = new TimeSpan(0, 0, ReconectSecond);
                    _Timer.Start();
                }
            );
        }

        private void StartReconnectCallback(object sender, EventArgs e)
        {
            _Timer.Stop();
            Connect();
        }

        private void Log(string str)
        {
            // Update data on the form
            this.Dispatcher.Invoke(
                delegate 
                {
                    lbLog.Items.Insert(0, DateTime.Now.ToString() + "\t" + str);
                }
            );
        }

        private void WriteMessageChat(string str)
        {
            // Update data on the form
            this.Dispatcher.Invoke(
                delegate
                {
                    lbChat.Items.Insert(0, DateTime.Now.ToString() + "\t" + str);
                }
            );
        }

        private void sendTextAndClear()
        {
            _client.SendString(edtText.Text);
            edtText.Text = "";
        }

        public void Connect()
        {
            if (!_client.Connected)
            {
                Log("Попытка подключение к " + edtIP.Text + ":" + edtPort.Text);
                int port = 20500;
                int.TryParse(edtPort.Text, out port);
                _client.Connect(edtIP.Text, port);
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            Reconnect = true;
            Connect();
        }

        private void SetStatusControl()
        {
            // Update data on the form
            this.Dispatcher.Invoke(
                delegate
                {
                    btnConnect.IsEnabled = !_client.Connected;
                    btnDisconnect.IsEnabled = !btnConnect.IsEnabled;
                    btnSend.IsEnabled = btnDisconnect.IsEnabled;

                    string imgBall = "Resources/BallRed.png";
                    if (_client.Connected)
                    {
                        imgBall = "Resources/BallGreen.png";
                    }
                    imgConnect.Source = new BitmapImage(new Uri(imgBall, UriKind.Relative));
                }
            );
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_client.Connected)
            {
                _client.Disconnect();
            }

            _notifier.Visible = false;
            _notifier.Dispose();
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
            Reconnect = false;
            _client.Disconnect();
        }

        public void setVisibleLayer(RadioButton btn)
        {
            if (btn == btnChat)
            {
                layChat.Visibility = Visibility.Visible;
                layLogs.Visibility = Visibility.Hidden;
                layProperty.Visibility = Visibility.Hidden;
            }
            else if (btn == btnLogs)
            {
                layChat.Visibility = Visibility.Hidden;
                layLogs.Visibility = Visibility.Visible;
                layProperty.Visibility = Visibility.Hidden;
            }
            else if (btn == btnProperties)
            {
                layChat.Visibility = Visibility.Hidden;
                layLogs.Visibility = Visibility.Hidden;
                layProperty.Visibility = Visibility.Visible;
            }
        }

        private void btn_Checked(object sender, RoutedEventArgs e)
        {
            setVisibleLayer((RadioButton)sender);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            btnChat.IsChecked = true;
            if (edtIP.Text != "")
            {
                Connect();
            }
        }
    }
}
