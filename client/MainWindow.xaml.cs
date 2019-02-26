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
using server.Classes;

namespace client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string strConnect = "Соединение установлено";
        public const string strDisconnect = "Соединение прекращено";
        public const int ReconectSecond = 10;

        private TClientSocket _client;
        private WinForms.NotifyIcon _notifier = new WinForms.NotifyIcon();
        private DispatcherTimer _Timer;
        private bool Reconnect = true;

        public MainWindow()
        {
            InitializeComponent();

            _client = new TClientSocket();
            _client.OnConnect = OnConnect;
            _client.OnDisconnect = OnDisconnect;
            _client.OnReceive = OnReceive;
            _client.OnError = OnError;
            _client.OnLog = Log;

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

        private void OnConnect(TSocket client)
        {
            SetStatusControl();
            Log(strConnect);
            ShowBalloon(strConnect);
        }

        private void OnDisconnect(TSocket client)
        {
            Log(strDisconnect);
            SetStatusControl();
            ShowBalloon(strDisconnect);
        }

        private void OnReceive(TSocket client, string str)
        {
            WriteMessageChat(str);
            ShowBalloon(str);
        }

        private void OnError(TSocket client, Exception e)
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
