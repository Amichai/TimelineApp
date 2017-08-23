using System;
using System.Threading;

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

        private readonly Timer timer;
        private TimeSpan? currentTime;
        private DateTime? lastUpdate;

        public Clock()
        {
            timer = new Timer(state => Tick(), null, 0, 10);
        }

        public void Seek(TimeSpan time)
        {
            IsPlaying = false;
            currentTime = time;
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
            OnNewTimeValue(new NewTimeValueEventArg(currentTime.Value));
        }

        private void OnNewTimeValue(NewTimeValueEventArg eventArgs)
        {
            NewTimeValue?.Invoke(this, eventArgs);
        }
    }
}