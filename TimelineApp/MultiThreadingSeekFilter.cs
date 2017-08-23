using System;

namespace TimelineApp
{
    internal sealed class MultiThreadingSeekFilter
    {
        public TimeSpan CurrentValue => currentTimeValue ?? TimeSpan.Zero;

        private int dropCount;
        private TimeSpan? currentTimeValue;
        private readonly TimeSpan continuityThreshold;
        private readonly int dropThreshold;
        private readonly object @lock = new object();

        public MultiThreadingSeekFilter(TimeSpan continuityThreshold, int dropThreshold)
        {
            this.continuityThreshold = continuityThreshold;
            this.dropThreshold = dropThreshold;
        }

        public void Filter(TimeSpan newTimeValue, bool isBackgroundThread)
        {
            lock (@lock)
            {
                if (isBackgroundThread)
                {
                    if (currentTimeValue == null)
                    {
                        UpdateCurrentTime(newTimeValue);
                        return;
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
        }

        private void UpdateCurrentTime(TimeSpan time)
        {
            currentTimeValue = time;
            dropCount = 0;
        }
    }
}