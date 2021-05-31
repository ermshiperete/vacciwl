// Copyright (c) 2021 Eberhard Beilharz
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;

namespace VaccinationAppointmentScheduler
{
	static class Program
	{
		private static void Main(string[] args)
		{
			var options = new Options();
			if (!options.HasConfig)
			{
				if (string.IsNullOrEmpty(options.Username))
				{
					options.Username = ReadFromUser("Username: ", options.Username);
				}

				if (string.IsNullOrEmpty(options.Password))
				{
					options.Password = ReadFromUser("Password: ", options.Password);
				}
				options.VaccinationCenter = ReadFromUser($"Vaccination Center ({options.VaccinationCenter}): ",
					options.VaccinationCenter);

				options.Url = ReadFromUser($"Url ({options.Url}): ", options.Url);

				var headlessString = ReadFromUser($"Headless ({options.Headless}): ", options.Headless.ToString());
				options.Headless = headlessString.ToLower() == "true";

				options.Save();
			}

			if (string.IsNullOrEmpty(options.Username) || string.IsNullOrEmpty(options.Password))
			{
				Console.WriteLine("Invalid username or password");
				return;
			}

			var manager = new VaccinationManager(options);
			manager.Main();
		}

		private static string ReadFromUser(string prompt, string oldValue)
		{
			Console.Write(prompt);
			var value = Console.ReadLine();
			return string.IsNullOrEmpty(value) ? oldValue : value;
		}
	}
}
