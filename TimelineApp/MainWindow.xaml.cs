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
        private int count;

        public MainWindow()
        {
            InitializeComponent();

            clock = new Clock();

            clock.NewTimeValue += ClockOnNewTimeValue;

            clock.Play();

            var tester = new BlackBoxTester(this);
            tester.Start();
        }

        public void PlayPause()
        {
            if (clock.IsPlaying)
            {
                clock.Pause();
            }
            else
            {
                maxVal = TimeSpan.MaxValue;
                clock.Play();
            }
        }

        public void Seek()
        {
            var seekTime = TimeSpan.FromSeconds(1);
            clock.Seek(seekTime);
            maxVal = seekTime;
        }

        private void ClockOnNewTimeValue(object sender, NewTimeValueEventArg arg)
        {
            Thread.Sleep(10);

            UpdateUI();

            if (arg.Time > maxVal)
            {
                Debug.Print("Spurious time value");
            }

            TimeValue = arg.Time;
        }

        private void UpdateUI()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                count++;
                Debug.Print($"Count: {count++}");
            }));
        }

        private void Seek_OnClick(object sender, RoutedEventArgs e)
        {
            Seek();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Pause_OnClick(object sender, RoutedEventArgs e)
        {
            PlayPause();
        }
    }
}
