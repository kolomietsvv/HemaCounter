using System;
using System.Diagnostics;
using System.Threading;

namespace HEMA
{
	public class Fight
	{
		private Stopwatch stopwatch;
		private Timer timer;

		public bool IsTimerStarted { get; private set; }
		public int BlueScore { get; set; }
		public int RedScore { get; set; }
		public int DoubleHits { get; set; }
		public int RedViolations { get; set; }
		public int BlueViolations { get; set; }
		public TimeSpan Elapsed => stopwatch.Elapsed;
		public bool IsStarted => stopwatch.Elapsed != TimeSpan.Zero;

		public Fight(TimerCallback timerElapsed)
		{
			stopwatch = new Stopwatch();
			timer = new Timer(timerElapsed, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
			timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
		}

		public void StopTimer()
		{
			timer.Change(0, Timeout.Infinite);
			stopwatch.Stop();
			IsTimerStarted = false;
		}

		public void StartTimer()
		{
			timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
			stopwatch.Start();
			IsTimerStarted = true;
		}

		public void Reset()
		{
			stopwatch.Reset();
			IsTimerStarted = false;
			BlueScore = 0;
			RedScore = 0;
			DoubleHits = 0;
			RedViolations = 0;
			BlueViolations = 0;
		}
	}
}
