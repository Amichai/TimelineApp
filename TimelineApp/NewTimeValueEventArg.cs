using System;

namespace TimelineApp
{
    internal sealed class NewTimeValueEventArg : EventArgs
    {
        public TimeSpan Time
        {
            get;
        }

        public int ResetCount
        {
            get;
        }

        public NewTimeValueEventArg(TimeSpan time, int resetCount)
        {
            Time = time;
            ResetCount = resetCount;
        }
    }
}