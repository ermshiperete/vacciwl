// Copyright (c) 2021 Eberhard Beilharz
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)

using System;

namespace VaccinationAppointmentScheduler.Logging
{
	public class ConsoleLogger: ILog
	{
		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}

		public void Message(string msg)
		{
			Console.WriteLine(msg);
		}
	}
}