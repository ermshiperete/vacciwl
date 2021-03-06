// Copyright (c) 2021 Eberhard Beilharz
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.IO;

namespace VaccinationAppointmentScheduler.Logging
{
	public class FileLogger: ILog
	{
		private readonly StreamWriter _writer;

		public FileLogger(string filename)
		{
			_writer = new StreamWriter(filename, true);
		}

		public void Message(string msg)
		{
			var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
			_writer.WriteLine($"{timestamp}: {msg}");
			_writer.Flush();
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_writer?.Dispose();
		}
	}
}