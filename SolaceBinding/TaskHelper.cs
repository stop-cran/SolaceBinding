using System;
using System.Threading.Tasks;

namespace Solace.Channels
{
    public static class TaskHelper
    {
        public static Task CreateTask(Action action, AsyncCallback callback, object state)
        {
            var res = new Task(x => action(), state);

            res.ContinueWith(t =>
            {
                t.Exception?.ToString(); // to avoid UnobservedTaskException

                try
                {
                    callback(t);
                }
                catch (ObjectDisposedException)
                { }
            });

            res.Start();

            return res;
        }

        public static Task CreateTask<T>(Func<T> action, AsyncCallback callback, object state)
        {
            var res = new Task<T>(x => action(), state);

            res.ContinueWith(t =>
            {
                t.Exception?.ToString(); // to avoid UnobservedTaskException

                try
                {
                    callback(t);
                }
                catch (ObjectDisposedException)
                { }
            });

            res.Start();

            return res;
        }
    }
}
