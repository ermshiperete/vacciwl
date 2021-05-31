using System;

namespace VaccinationAppointmentScheduler
{
	public class MultiLogger: ILog
	{
		private ILog _otherLogger;

		public MultiLogger(ILog other)
		{
			_otherLogger = other;
		}

		public void Message(string msg)
		{
			_otherLogger.Message(msg);
			Console.WriteLine(msg);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_otherLogger.Dispose();
		}
	}
}