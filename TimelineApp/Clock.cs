using System;
using System.Threading;
using System.Threading.Tasks;

namespace TimelineApp
{
    internal sealed class Clock
    {
        public event EventHandler<NewTimeValueEventArg> NewTimeValue;

        public bool IsPlaying
        {
            get;
            private set;
        }

        private TimeSpan? currentTime;
        private DateTime? lastUpdate;
        private readonly object @lock = new object();

        public Clock()
        {
            new TaskFactory().StartNew(() =>
            {
                while (true)
                {
                    Tick();
                    Thread.Sleep(10);
                }
            }, TaskCreationOptions.LongRunning);
        }

        public void Seek(TimeSpan time)
        {
            lock (@lock)
            {
                IsPlaying = false;
                currentTime = time;
                NotifyNewTimeValue();
            }
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public void Play()
        {
            IsPlaying = true;
        }

        private void Tick()
        {
            lock (@lock)
            {
                if (!IsPlaying)
                {
                    lastUpdate = null;
                    return;
                }

                if (currentTime == null)
                {
                    currentTime = TimeSpan.Zero;
                }

                var diff = DateTime.Now - (lastUpdate ?? DateTime.Now);
                currentTime += diff;
                lastUpdate = DateTime.Now;
                NotifyNewTimeValue();
            }
        }

        private void NotifyNewTimeValue()
        {
            OnNewTimeValue(new NewTimeValueEventArg(currentTime.Value));
        }

        private void OnNewTimeValue(NewTimeValueEventArg eventArgs)
        {
            NewTimeValue?.Invoke(this, eventArgs);
        }
    }
}