using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Channels;
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

namespace TimelineApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private TimeSpan timeValue;
        public TimeSpan TimeValue
        {
            get
            {
                return timeValue;
            }
            set
            {
                timeValue = value;
                OnPropertyChanged();
            }
        }

        private readonly Clock clock;

        public MainWindow()
        {
            InitializeComponent();

            clock = new Clock();

            clock.NewTimeValue += (sender, arg) =>
            {
                TimeValue = arg.Time;

                if (timeValue < TimeSpan.FromSeconds(.1))
                {
                    maxVal = TimeSpan.MaxValue;
                }

                if (arg.Time > maxVal)
                {
                    Debug.Print($"{arg.Time}");
                }
            };

            clock.Play();
        }

        private TimeSpan maxVal = TimeSpan.MaxValue;

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            maxVal = TimeValue;
            ComplexTimelineComputation();
            clock.Reset();
        }

        private void ComplexTimelineComputation()
        {
            Thread.Sleep(10);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
