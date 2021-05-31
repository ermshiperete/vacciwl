using System;

namespace VaccinationAppointmentScheduler
{
	public interface ILog: IDisposable
	{
		void Message(string msg);
	}
}