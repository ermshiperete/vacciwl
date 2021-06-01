// Copyright (c) 2021 Eberhard Beilharz
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;

namespace VaccinationAppointmentScheduler.Logging
{
	public interface ILog: IDisposable
	{
		void Message(string msg);
	}
}