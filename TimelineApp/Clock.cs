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

        public bool IsPlaying;

        public int ResetCounter
        {
            get;
            private set;
        }

        private readonly Timer timer;
        public Clock()
        {
            timer = new Timer(state => Tick(), null, 0, 10);
        }

        public void Seek(TimeSpan time)
        {
            currentTime = time;
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public void Play()
        {
            IsPlaying = true;
        }

        private bool resetInFlight;

        public TimeSpan Reset()
        {
            lock (@lock)
            {
                var val = currentTime.Value;
                currentTime = TimeSpan.Zero;
                ResetCounter++;
                resetInFlight = true;
                return val;
            }
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

                if (resetInFlight)
                {
                    NotifyResetComplete();
                    resetInFlight = false;
                }

                var diff = DateTime.Now - (lastUpdate ?? DateTime.Now);
                currentTime += diff;
                lastUpdate = DateTime.Now;
                NotifyNewTimeValue();
            }
        }

        private readonly object @lock = new object();

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