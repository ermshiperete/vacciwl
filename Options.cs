using System;
using System.IO;
using IniFile;

namespace VaccinationAppointmentScheduler
{
	public class Options
	{
		private readonly Ini _iniFile;

		public Options()
		{
			_iniFile = HasConfig ? new Ini(Filename) : new Ini();
		}

		private string Filename => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"vacciwl", "config.ini");

		public bool HasConfig => File.Exists(Filename);

		public string Username
		{
			get
			{
				var user = Environment.GetEnvironmentVariable("ITM_USER");
				return !string.IsNullOrEmpty(user) ? user : Value("Credentials", "Username", string.Empty);
			}
			set => SetValue("Credentials", "Username", value);
		}

		public string Password
		{
			get
			{
				var password = Environment.GetEnvironmentVariable("ITM_PW");
				return !string.IsNullOrEmpty(password) ? password : Value("Credentials", "Password", string.Empty);
			}
			set => SetValue("Credentials", "Password", value);

		}

		public string VaccinationCenter
		{
			get => Value("Settings", "VaccinationCenter", "siegen-wittgenstein");
			set => SetValue("Settings", "VaccinationCenter", value);
		}

		public string Url
		{
			get => Value("Settings", "Url", "https://itm-wl.service-now.com/vam");
			set => SetValue("Settings", "Url", value);
		}

		public bool Headless
		{
			get => Value("Settings", "Headless");
			set => SetValue("Settings", "Headless", value);
		}

		public bool BookAppointment { get; } = true;
		public bool CheckAllCenters { get; } = false;
		public bool Debug { get; } = false;

		private string Value(string sectionName, string key, string defaultValue)
		{
			if (!_iniFile.TryGetValue(sectionName, out var section))
				return defaultValue;

			var value = section[key];
			return string.IsNullOrEmpty(value) ? defaultValue : value;
		}

		private bool Value(string sectionName, string key)
		{
			if (!_iniFile.TryGetValue(sectionName, out var section))
				return false;

			if (string.IsNullOrEmpty(section[key]))
				return false;
			bool value = section[key];
			return value;
		}

		private void SetValue(string sectionName, string key, string value)
		{
			if (!_iniFile.TryGetValue(sectionName, out var section))
			{
				section = new IniFile.Section(sectionName);
				_iniFile.Add(section);
			}

			section[key] = value;
		}

		private void SetValue(string sectionName, string key, bool value)
		{
			if (!_iniFile.TryGetValue(sectionName, out var section))
			{
				section = new IniFile.Section(sectionName);
				_iniFile.Add(section);
			}

			section[key] = value;
		}

		public void Save()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(Filename));
			File.WriteAllText(Filename, _iniFile.ToString());
		}
	}
}