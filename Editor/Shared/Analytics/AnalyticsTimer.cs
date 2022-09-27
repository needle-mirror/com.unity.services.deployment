// WARNING: Auto generated code by Starbuck2. Modifications will be lost!
using System;
using System.Diagnostics;

namespace Unity.Services.Deployment.Editor.Shared.Analytics
{
    class AnalyticsTimer : IAnalyticsTimer
    {
        readonly Stopwatch m_Stopwatch;
        readonly Action<int> m_DurationHandler;

        public AnalyticsTimer(Action<int> durationHandler)
        {
            m_Stopwatch = new Stopwatch();
            m_DurationHandler = durationHandler;
            m_Stopwatch.Start();
        }

        public void End()
        {
            m_Stopwatch.Stop();
            m_DurationHandler((int)m_Stopwatch.ElapsedMilliseconds);
        }
    }
}
