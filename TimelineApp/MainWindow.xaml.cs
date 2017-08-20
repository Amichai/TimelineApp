using System;
using System.ComponentModel;
using System.Diagnostics;
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
        private readonly object syncRoot = new object();

        private int count = 0;
        private bool isReset;

        public MainWindow()
        {
            InitializeComponent();

            clock = new Clock();

            clock.NewTimeValue += ClockOnNewTimeValue;
            clock.ResetComplete += (sender, args) =>
            {
                isReset = false;
            };

            clock.Play();
        }

        private void ClockOnNewTimeValue(object sender, NewTimeValueEventArg arg)
        {
            lock (syncRoot)
            {
                TimeValue = arg.Time;

                UpdateUI();

                if (arg.Time > maxVal && !isReset)
                {
                    Debug.Print($"{arg.Time}");
                }
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            lock (syncRoot)
            {
                maxVal = TimeValue;
                if (ComplexTimelineComputation())
                {
                    clock.Reset();
                    isReset = true;
                }
            }
        }

        private static bool ComplexTimelineComputation()
        {
            Thread.Sleep(10);
            return true;
        }

        private void UpdateUI()
        {
            Dispatcher.Invoke(() =>
            {
                count++;
            });
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
