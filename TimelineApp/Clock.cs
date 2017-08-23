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

        public TimeSpan? CurrentTime
        {
            get;
            private set;
        }

        private DateTime? lastUpdate;

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
            IsPlaying = false;
            CurrentTime = time;
            NotifyNewTimeValue();
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
            if (!IsPlaying)
            {
                lastUpdate = null;
                return;
            }

            if (CurrentTime == null)
            {
                CurrentTime = TimeSpan.Zero;
            }

            var diff = DateTime.Now - lastUpdate;
            CurrentTime += diff ?? TimeSpan.Zero;
            lastUpdate = DateTime.Now;
            NotifyNewTimeValue();
        }

        private void NotifyNewTimeValue()
        {
            OnNewTimeValue(new NewTimeValueEventArg(CurrentTime.Value));
        }

        private void OnNewTimeValue(NewTimeValueEventArg eventArgs)
        {
            NewTimeValue?.Invoke(this, eventArgs);
        }
    }
}