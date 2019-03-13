using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace client
{
    /// <summary>
    /// Interaction logic for MiniWindow.xaml
    /// </summary>
    public partial class MiniWindow : Window
    {
        private DispatcherTimer _Timer;
        private int _secondOpenWindow;
        private string _title;
        private TFlowDocument _fd;
        private bool _canClose = false;

        public MiniWindow()
        {
            InitializeComponent();

            _Timer = new DispatcherTimer();
            _Timer.Tick += TimerCallback;
            _Timer.Interval = new TimeSpan(0, 0, 1);

            _fd = new TFlowDocument(lbChat.Document);
            _title = App.Current.MainWindow.Title;

            Topmost = true;
            WindowStyle = WindowStyle.ToolWindow;

            SetPosition();
        }

        public void SetPosition()
        {
            // set position window
            System.Drawing.Size resolution = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Size;
            this.Left = resolution.Width - this.Width;
            this.Top = resolution.Height - this.Height;
        }

        private void TimerCallback(object sender, EventArgs e)
        {
            if (_secondOpenWindow > 0)
            {
                _secondOpenWindow--;
                this.Title = string.Format("{0} ({1})", _title, _secondOpenWindow);
            }
            else 
            {
                Hide();
                _Timer.Stop();
            }
        }

        public void Show(string str, int secondOpenWindow = 10)
        {
            if (_Timer.IsEnabled)
            {
                _secondOpenWindow = _secondOpenWindow + secondOpenWindow;
            }
            else
            {
                _secondOpenWindow = secondOpenWindow;
                _Timer.Start();
            }

            _fd.Write(str);
            
            base.Show();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_canClose)
            {
                e.Cancel = true;
                Hide();
            }
        }

        public new void Close()
        {
            _canClose = true;
            base.Close();
        }
   }
}
