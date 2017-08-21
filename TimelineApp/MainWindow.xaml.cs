using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;

namespace TimelineApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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
        private TimeSpan maxVal = TimeSpan.MaxValue;

        private int count = 0;

        private readonly object @lock = new object();

        public MainWindow()
        {
            InitializeComponent();

            clock = new Clock();

            clock.NewTimeValue += ClockOnNewTimeValue;
            //clock.ResetComplete += (sender, arg) => maxVal = TimeSpan.MaxValue;

            clock.Play();

            var tester = new BlackBoxTester(this);
            tester.Start();
        }

        public void ButtonClick()
        {
            if (ComplexTimelineComputation())
            {
                lock (@lock)
                {
                    maxVal = TimeValue;
                    clock.Reset();
                }
            }
        }

        private void ClockOnNewTimeValue(object sender, NewTimeValueEventArg arg)
        {
            lock (@lock)
            {
                if (arg.ResetCount != clock.ResetCounter)
                {
                    return;
                }

                TimeValue = arg.Time;

                UpdateUI();

                if (arg.Time < TimeSpan.FromMilliseconds(50))
                {
                    maxVal = TimeSpan.MaxValue;
                }

                if (arg.Time > maxVal)
                {
                    throw new Exception("Spurious time value");
                }
            }

        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ButtonClick();
        }

        private static bool ComplexTimelineComputation()
        {
            Thread.Sleep(100);
            return true;
        }

        private void UpdateUI()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                count++;
            }));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
