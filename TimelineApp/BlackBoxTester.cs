using System;
using System.Threading;
using System.Threading.Tasks;

namespace TimelineApp
{
    internal sealed class BlackBoxTester
    {
        private readonly MainWindow window;
        private int count = 0;

        public BlackBoxTester(MainWindow window)
        {
            this.window = window;
        }

        public void Start()
        {
            new TaskFactory().StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(10);
                    window.Dispatcher.Invoke(() =>
                    {
                        window.ButtonClick();
                    });
                }
            }, TaskCreationOptions.LongRunning);

            new TaskFactory().StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(50);
                    var task = Task.Run(() => TestForDeadlock());
                    if (!task.Wait(TimeSpan.FromSeconds(1)))
                    {
                        Environment.FailFast("Deadlock");
                    }
                }
            }, TaskCreationOptions.LongRunning);
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