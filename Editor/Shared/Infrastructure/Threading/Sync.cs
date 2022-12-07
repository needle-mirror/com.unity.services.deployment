// WARNING: Auto generated code. Modifications will be lost!
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.Deployment.Editor.Shared.Infrastructure.Threading
{
    static class Sync
    {
        public static T RunInBackgroundThread<T>(Func<Task<T>> action)
        {
            var res = default(T);
            var thread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                res = action().Result;
            });
            thread.Start();
            thread.Join();
            return res;
        }
    }
}
