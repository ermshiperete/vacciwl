// Copyright (c) 2021 Eberhard Beilharz
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;

namespace VaccinationAppointmentScheduler.Logging
{
	public class MultiLogger: ILog
	{
		private ILog[] _loggers;

		public MultiLogger(ILog[] loggers)
		{
			_loggers = loggers;
		}

		public void Message(string msg)
		{
			foreach (var logger in _loggers)
				logger.Message(msg);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			foreach (var logger in _loggers)
				logger.Dispose();
		}
	}
}