// <copyright file="PerformanceTimer.cs" company="recogniser project contributors">
// Copyright (c) 2025 recogniser project contributors.
// Licensed under the AGPL-3.0-or-later license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Recogniser
{
    using System.Collections.Concurrent;

    internal sealed class PerformanceTimer
    {
        private readonly string name;
        private readonly ConcurrentDictionary<long, long> startEvents = new();
        private readonly ConcurrentDictionary<long, long> stopEvents = new();

        public PerformanceTimer(string name)
        {
            this.name = name;
        }

        public void Start(long id)
        {
            startEvents[id] = DateTime.Now.Ticks;
        }

        public void Stop(long id)
        {
            stopEvents[id] = DateTime.Now.Ticks;
        }

        public void Cancel(long id)
        {
            startEvents.Remove(id, out _);
        }

        public string GetSummary()
        {
            long min = long.MaxValue;
            long max = long.MinValue;
            long ave;
            long sum = 0;
            long count = 0;

            foreach (long id in startEvents.Keys)
            {
                long startTicks = startEvents[id];
                long milliseconds = -1;

                if (stopEvents.TryGetValue(id, out long stopTicks))
                {
                    milliseconds = (stopTicks - startTicks) / TimeSpan.TicksPerMillisecond;
                    sum += milliseconds;
                    count++;

                    if (milliseconds < min)
                    {
                        min = milliseconds;
                    }

                    if (milliseconds > max)
                    {
                        max = milliseconds;
                    }
                }
            }

            if (count > 0)
            {
                ave = sum / count;
            }
            else
            {
                min = max = ave = 0;
            }

            return $"{name}\t{min / 1000.0}\t{max / 1000.0}\t{ave / 1000.0}\t{sum / 1000.0}\t{count}";
        }
    }
}
