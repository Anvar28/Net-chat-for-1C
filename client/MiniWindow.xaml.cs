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

        public MiniWindow(Window _Owner)
        {
            InitializeComponent();

            this.Owner = _Owner;

            _Timer = new DispatcherTimer();
            _Timer.Tick += TimerCallback;
            _Timer.Interval = new TimeSpan(0, 0, 1);

            _fd = new TFlowDocument(lbChat.Document);
            _title = App.Current.MainWindow.Title;

            SetPosition();
        }

        public void SetPosition()
        {
            // set position window
            System.Drawing.Size resolution = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
            this.Left = resolution.Width - this.Width;
            this.Top = resolution.Height - this.Height - 50;
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
    }
}
