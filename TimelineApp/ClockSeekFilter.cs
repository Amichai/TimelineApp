using System;

namespace TimelineApp
{
    internal sealed class ClockSeekFilter
    {
        public TimeSpan CurrentValue => currentTimeValue ?? TimeSpan.Zero;

        private int dropCount;
        private TimeSpan? currentTimeValue;
        private readonly TimeSpan continuityThreshold;
        private const int dropThreshold = 3;
        private readonly object @lock = new object();

        public ClockSeekFilter(TimeSpan continuityThreshold)
        {
            this.continuityThreshold = continuityThreshold;
        }

        public TimeSpan Filter(TimeSpan newTimeValue, bool isBackgroundThread)
        {
            lock (@lock)
            {
                if (isBackgroundThread)
                {
                    if (currentTimeValue == null)
                    {
                        UpdateCurrentTime(newTimeValue);
                        return currentTimeValue.Value;
                    }

                    var diff = newTimeValue - currentTimeValue.Value;
                    if (diff < continuityThreshold)
                    {
                        UpdateCurrentTime(newTimeValue);
                    }
                    else if (++dropCount > dropThreshold)
                    {
                        UpdateCurrentTime(newTimeValue);
                    }
                }
                else
                {
                    UpdateCurrentTime(newTimeValue);
                }
            }

            return currentTimeValue.Value;
        }

        private void UpdateCurrentTime(TimeSpan time)
        {
            currentTimeValue = time;
            dropCount = 0;
        }
    }
}