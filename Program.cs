// Copyright (c) 2021 Eberhard Beilharz
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;

namespace VaccinationAppointmentScheduler
{
	static class Program
	{
		private static void Main(string[] args)
		{
			var user = Environment.GetEnvironmentVariable("ITM_USER");
			var password = Environment.GetEnvironmentVariable("ITM_PW");
			if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
			{
				Console.WriteLine("Please set username and password in ITM_USER and ITM_PW environment variables!");
				return;
			}

			var manager = new VaccinationManager("https://itm-wl.service-now.com/vam", false);
			manager.Main(user, password, "siegen-wittgenstein");
		}
	}
}
