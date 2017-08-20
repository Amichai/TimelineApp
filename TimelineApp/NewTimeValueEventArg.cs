using System;

namespace TimelineApp
{
    internal sealed class NewTimeValueEventArg : EventArgs
    {
        public TimeSpan Time
        {
            get;
        }

        public NewTimeValueEventArg(TimeSpan time)
        {
            Time = time;
        }
    }
}