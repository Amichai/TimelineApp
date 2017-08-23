using System;
using System.Threading;
using System.Threading.Tasks;

namespace TimelineApp
{
    internal sealed class BlackBoxTester
    {
        private readonly MainWindow window;
        private int count = 0;
        private Timer t1, t2;
        public BlackBoxTester(MainWindow window)
        {
            this.window = window;
        }

        public void Start()
        {
            t1 = new Timer(state =>
            {
                window.Dispatcher.Invoke(() =>
                {
                    window.Seek();
                    Thread.Sleep(10);
                    window.PlayPause();
                });
            }, null, 0, 500);

            t2 = new Timer(state =>
            {
                var task = Task.Run(() => TestForDeadlock());
                if (!task.Wait(TimeSpan.FromSeconds(1)))
                {
                    Environment.FailFast("Deadlock");
                }
            }, null, 0, 50);
        }

        private void TestForDeadlock()
        {
            window.Dispatcher.Invoke(() =>
            {
                count++;
            });
        }
    }
}