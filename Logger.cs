using System;
using System.IO;

namespace VaccinationAppointmentScheduler
{
	public class Logger: ILog
	{
		private readonly StreamWriter _writer;

		public Logger(string filename)
		{
			_writer = new StreamWriter(filename);
		}

		public void Message(string msg)
		{
			_writer.WriteLine(msg);
			_writer.Flush();
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_writer?.Dispose();
		}
	}
}