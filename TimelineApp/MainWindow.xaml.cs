using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
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
            Observable.FromEventPattern<NewTimeValueEventArg>(handler => clock.NewTimeValue += handler,
                handler => clock.NewTimeValue -= handler)
                .Select(arguments => arguments.EventArgs)
                .Select(eventArg => continuityFilter.Filter(eventArg.Time, Thread.CurrentThread.IsBackground))
                .Subscribe(time => ProcessNewTimeValue());
            clock.Play();
        }

        private bool isPlaying;
        private readonly MultiThreadingSeekFilter continuityFilter = new MultiThreadingSeekFilter(TimeSpan.FromSeconds(.1), dropThreshold:2);

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
            var time = TimeSpan.FromSeconds(1);
            clock.Seek(time);
            maxVal = time;
        }

        private void ProcessNewTimeValue()
        {
            Thread.Sleep(10);

            UpdateUI();

            if (continuityFilter.CurrentValue > maxVal)
            {
                Debug.Print("Spurious time value");
            }

            TimeValue = continuityFilter.CurrentValue;
        }

        private void UpdateUI()
        {
            Dispatcher.Invoke(new Action(() =>
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
