using System;
using System.Threading;
using System.Threading.Tasks;

namespace TimelineApp
{
    internal sealed class Clock
    {
        public event EventHandler<NewTimeValueEventArg> NewTimeValue;
        public event EventHandler<EventArgs> ResetComplete;

        private TimeSpan? currentTime;
        private DateTime? lastUpdate;
        private bool isPlaying;

        public int ResetCounter
        {
            get;
            private set;
        }

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
            currentTime = time;
        }

        public void Pause()
        {
            isPlaying = false;
        }

        public void Play()
        {
            isPlaying = true;
        }

        public void Reset()
        {
            currentTime = TimeSpan.Zero;
            ResetCounter++;
        }

        private void Tick()
        {
            if (!isPlaying)
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

        private void NotifyNewTimeValue()
        {
            OnNewTimeValue(new NewTimeValueEventArg(currentTime.Value, ResetCounter));
        }

        private void OnNewTimeValue(NewTimeValueEventArg eventArgs)
        {
            NewTimeValue?.Invoke(this, eventArgs);
        }

        private void NotifyResetComplete()
        {
            OnResetComplete(new EventArgs());
        }

        private void OnResetComplete(EventArgs eventArgs)
        {
            ResetComplete?.Invoke(this, eventArgs);
        }
    }
}