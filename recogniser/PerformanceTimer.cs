using System.Collections.Concurrent;
using System.Diagnostics;

namespace recogniser
{
	public class PerformanceTimer
	{
		private readonly string _name;
		private readonly ConcurrentDictionary<long, long> _startEvents = new();
        private readonly ConcurrentDictionary<long, long> _stopEvents = new();

        public PerformanceTimer(string name)
		{
			_name = name;
		}

		public void Start(long id)
		{
			_startEvents[id] = DateTime.Now.Ticks;
		}

		public void Stop(long id)
		{
			_stopEvents[id] = DateTime.Now.Ticks;
        }

		public void Cancel(long id)
		{
			if (_startEvents.ContainsKey(id))
			{
                _startEvents.Remove(id, out _);
			}
		}

		public string GetSummary()
		{
			long min = long.MaxValue;
			long max = long.MinValue;
			long ave;
			long sum = 0;
			long count = 0;

			foreach (long id in _startEvents.Keys)
			{
				long startTicks = _startEvents[id];
				long milliseconds = -1;

				if (_stopEvents.TryGetValue(id, out long stopTicks))
				{
					milliseconds = (stopTicks - startTicks) / TimeSpan.TicksPerMillisecond;
					sum += milliseconds;
					count++;

					if (milliseconds < min)
						min = milliseconds;
					if (milliseconds > max)
						max = milliseconds;
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

            return $"{_name}\t{min / 1000.0}\t{max / 1000.0}\t{ave / 1000.0}\t{sum / 1000.0}\t{count}";
		}
	}
}
