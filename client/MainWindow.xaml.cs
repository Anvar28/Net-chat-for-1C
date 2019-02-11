using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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

    delegate void ReceiveData(string str);

    class TClient
    {
        private Socket _socket;
        private MemoryStream _ms;
        private BinaryWriter _writer;
        private BinaryReader _reader;
        private ReceiveData _ToReceiveData;

        public bool Connected { get { return _socket.Connected; } }

        public TClient(ReceiveData lToReceiveData)
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _ms = new MemoryStream(new byte[256], 0, 256, true, true);
            _writer = new BinaryWriter(_ms);
            _reader = new BinaryReader(_ms);
            _ToReceiveData = lToReceiveData;
        }

        public void Connect(string ip, int port)
        {
            _socket.Connect(IPAddress.Parse(ip), port);
            if (_socket.Connected)
            {
                //_socket.BeginReceive();
            }            
        }

        private void ClearBuff()
        {
            Array.Clear(_ms.GetBuffer(), 0, _ms.GetBuffer().Length);
            _ms.Position = 0;
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
            _socket.Send(_ms.ToArray());
        }

        public void Close()
        {
            //socket.Disconnect(true);
        }

        public void Receive()
        {
            Array.Clear(_ms.GetBuffer(), 0, _ms.GetBuffer().Length);
            _ms.Position = 0;
            _socket.Receive(_ms.GetBuffer());
            server.commands command = (server.commands)_reader.ReadInt32();
            switch (command)
            {
                case server.commands.sendString:
                    _ToReceiveData(_reader.ReadString());
                    break;
                case server.commands.sendFile:
                    new Exception("Прием файлов не реализован");
                    break;
                default:
                    break;
            }
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
            client = new TClient(ReceiveData);
        }

        private void log(string str)
        {
            lbLog.Items.Insert(0, DateTime.Now.ToString()+"\t"+str);
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
                try
                {
                    log("Попытк подключение к " + edtIP.Text + ":" + edtPort.Text);
                    int port = 20500;
                    int.TryParse(edtPort.Text, out port);
                    client.Connect(edtIP.Text, port);
                    log("Подключение прошло успешно.");
                    setStatusControl();
                }
                catch (Exception ex)
                {
                    log(ex.Message);
                }
            }
        }

        private void setStatusControl()
        {
            btnConnect.IsEnabled = !client.Connected;
            btnDisconnect.IsEnabled = !btnConnect.IsEnabled;
            btnSend.IsEnabled = btnDisconnect.IsEnabled;

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (client.Connected)
            {
                client.Close();
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

        private void ReceiveData(string str)
        {
            log(str);
        }
    }
}
