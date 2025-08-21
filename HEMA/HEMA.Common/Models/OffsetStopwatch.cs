using System;
using System.Diagnostics;

namespace HEMA.Models
{
	public class OffsetStopwatch
	{
		private readonly Stopwatch _sw = new Stopwatch();
		private readonly TimeSpan _offset;

		public OffsetStopwatch(TimeSpan offset)
		{
			_offset = offset;
		}

		public OffsetStopwatch():this(TimeSpan.Zero) { }

		public void Start() => _sw.Start();
		public void Stop() => _sw.Stop();
		public void Reset() => _sw.Reset();
		public bool IsRunning => _sw.IsRunning;

		public TimeSpan Elapsed => _offset + _sw.Elapsed;
	}
}
