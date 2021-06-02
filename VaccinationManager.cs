// Copyright (c) 2021 Eberhard Beilharz
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using VaccinationAppointmentScheduler.Logging;

namespace VaccinationAppointmentScheduler
{
	public class VaccinationManager: IDisposable
	{
		private const    int                            FirstShot  = 1;
		private const    int                            SecondShot = 2;
		private          FirefoxDriver                  _driver;
		private readonly Options                        _options;
		private readonly Dictionary<string, (int firstShotCount, int secondShotCount)> _statistics;
		private          string                         _currentCenter;

		public VaccinationManager(Options options)
		{
			_options = options;
			if (_options.Verbose)
			{
				Log = new MultiLogger(new ILog[] {
					new FileLogger(_options.Logfile),
					new ConsoleLogger()
				});
			}
			else
				Log = new FileLogger(_options.Logfile);

			_statistics = new Dictionary<string, (int, int)>();
		}

		private ILog Log { get; }

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Log.Dispose();
		}

		public void Main()
		{
			var options = new FirefoxOptions();
			if (!_options.Debug)
				options.LogLevel = FirefoxDriverLogLevel.Error;

			if (_options.Headless)
				options.AddArguments("--headless");

			_driver = new FirefoxDriver(".", options) { Url = _options.Url };
			try
			{
				Login(_options.Username, _options.Password);
				_driver.WaitAndFindElement(By.XPath("//button[contains(.,'Termin buchen')]")).Click();
				if (_options.CheckAllCenters)
				{
					for (var moreCenters = SelectFirstVaccinationCenter();
						moreCenters;
						moreCenters = SelectNextVaccinationCenter())
					{
						_driver.WaitForElement(By.ClassName("appointmentContentContainer"));
						TryBookSlot();
					}
				}
				else
				{
					SelectVaccinationCenter(_options.VaccinationCenter);

					_driver.WaitForElement(By.ClassName("appointmentContentContainer"));
					TryBookSlot();
				}
			}
			finally
			{
				_driver.Close();
				_driver.Quit();
				_driver.Dispose();
			}
		}

		private void Login(string user, string password)
		{
			if (_driver.FindElements(By.CssSelector(".sub-avatar")).Count != 0)
				return;

			_driver.FindElement(By.XPath("//button[contains(.,'Anmelden')]")).Click();
			_driver.FindElement(By.Id("username")).SendKeys(user);
			_driver.FindElement(By.Id("password")).SendKeys(password);
			_driver.FindElement(By.XPath("//button[contains(.,'Anmelden')]")).Click();
		}

		private void SelectVaccinationCenter(string vaccinationCenter)
		{
			_driver.WaitAndFindElement(By.Id("s2id_sp_formfield_preferred_center")).Click();
			_driver.WaitAndFindElement(By.Id("s2id_autogen8_search")).SendKeys(vaccinationCenter);
			IWebElement result;
			for (result = _driver.WaitAndFindElement(By.ClassName("select2-result-label"));
				!result.Text.Contains(vaccinationCenter, StringComparison.OrdinalIgnoreCase);
				result = _driver.WaitAndFindElement(By.ClassName("select2-result-label")))
			{
				Thread.Sleep(100);
			}

			_currentCenter = result.Text;
			Log.Message($"Checking {_currentCenter}:");
			_statistics[_currentCenter] = (0, 0);
			result.Click();
		}

		private bool SelectFirstVaccinationCenter()
		{
			_driver.WaitAndFindElement(By.Id("s2id_sp_formfield_preferred_center")).Click();
			var result = _driver.WaitAndFindElement(By.ClassName("select2-result-label"));

			_currentCenter = result.Text;
			Log.Message($"Checking {_currentCenter}:");
			_statistics[_currentCenter] = (0, 0);
			result.Click();
			return true;
		}

		private bool SelectNextVaccinationCenter()
		{
			_driver.WaitAndFindElement(By.Id("s2id_sp_formfield_preferred_center")).Click();
			var previous = _driver.WaitAndFindElement(By.XPath("//li[contains(@class, 'select2-highlighted')]/div/div"));
			_driver.WaitAndFindElement(By.Id("s2id_autogen8_search")).SendKeys(Keys.Down);
			var result = _driver.WaitAndFindElement(By.XPath("//li[contains(@class, 'select2-highlighted')]/div/div"));
			if (previous.Text == result.Text)
				return false;

			_currentCenter = result.Text;
			Log.Message($"Checking {_currentCenter}:");
			_statistics[_currentCenter] = (0, 0);
			result.Click();
			return true;
		}

		private void TryBookSlot()
		{
			var success = false;
			for (success = BookSlotsIfAvailable();
				!success && SelectNextDay(GetDivXPathForShot(FirstShot));
				)
			{
				success = BookSlotsIfAvailable();
			}
			if (!success)
				Log.Message("\tFinished. No slots on other days available.");
		}

		private bool BookSlotsIfAvailable()
		{
			var firstDivXPath = GetDivXPathForShot(FirstShot);
			var firstDate = GetSelectedDate(firstDivXPath);
			if (SelectNextAvailableSlot(FirstShot))
			{
				Log.Message($"\tFound available slot on {firstDate}, looking for slots for second shot...");
				var (count1, count2) = _statistics[_currentCenter];
				_statistics[_currentCenter] = (firstShotCount: count1 + 1, secondShotCount: count2);
				for (var success = SelectNextAvailableSecondSlot(firstDate);
					!success && SelectNextDay(GetDivXPathForShot(SecondShot));
					)
				{
					if (SelectNextAvailableSecondSlot(firstDate))
						return true;
				}
			}
			else
				Log.Message($"\tNo slots on {firstDate}");

			return false;
		}

		private bool SelectNextDay(string divXPath)
		{
			static bool SelectNextDayInRow(IReadOnlyCollection<IWebElement> readOnlyCollection)
			{
				if (readOnlyCollection.Count <= 0 || readOnlyCollection.First().IsDisabled())
					return false;

				readOnlyCollection.First().Click();
				Thread.Sleep(500);
				return true;
			}

			var nextDays = _driver.FindElements(By.XPath($"{divXPath}//td[contains(@class, 'selectedDate')]/following-sibling::td/button"));
			if (SelectNextDayInRow(nextDays))
				return true;

			nextDays = _driver.FindElements(By.XPath(
				$"{divXPath}//td[contains(@class, 'selectedDate')]/parent::tr/following-sibling::tr/td/button"));
			if (SelectNextDayInRow(nextDays))
				return true;

			// at end of month
			var goNext = _driver.WaitAndFindElement(By.XPath($"{divXPath}//button[@id = 'goNext']"));
			if (goNext.IsDisabled())
				return false;

			var monthElement = _driver.WaitAndFindElement(By.XPath("//div[@class='calHeader']/span"));
			var regex = new Regex("[0-9]+");
			var year = regex.Match(monthElement.Text).Captures[0].Value;
			if (int.Parse(year) > DateTime.Now.Year)
				return false;

			goNext.Click();
			Thread.Sleep(500);
			return true;
		}

		private string GetSelectedDate(string xpathForShot)
		{
			return _driver.WaitAndFindElement(By.XPath($"{xpathForShot}/descendant::td[contains(@class, 'selectedDate')]/button")).GetAttribute("aria-label");
		}

		private bool SelectNextAvailableSecondSlot(string firstDate)
		{
			var secondDivXPath = GetDivXPathForShot(SecondShot);
			var secondDate = GetSelectedDate(secondDivXPath);
			if (SelectNextAvailableSlot(SecondShot))
			{
				var (count1, count2) = _statistics[_currentCenter];
				_statistics[_currentCenter] = (firstShotCount: count1, secondShotCount: count2++);
				if (_options.BookAppointment)
				{
					Log.Message($"Congratulations! Reserved slots on {firstDate} and {secondDate}!");
					_driver.FindElement(By.XPath("//button[contains(.,'Absenden')]")).Click();
					return true;
				}
				Log.Message($"Found available slots on {firstDate} and {secondDate}!");
				return false;
			}
			Log.Message($"\t\tNo slots for second shot on {secondDate}");
			return false;
		}

		private bool SelectNextAvailableSlot(int shotNumber)
		{
			var shotDiv = _driver.WaitAndFindElement(By.XPath(GetDivXPathForShot(shotNumber)));
			var availableSlots = shotDiv.FindElements(By.ClassName("appointmentSlot"));

			if (availableSlots.Count <= 0)
				return false;

			availableSlots.First().Click();
			return true;
		}

		private static string GetDivXPathForShot(int shotNumber)
		{
			return $"//div[contains(@class, 'dosage') and position() = {shotNumber}]";
		}

		public void ShowStatistics()
		{
			if (!_options.ShowStatistics)
				return;

			foreach (var center in _statistics.Keys)
			{
				var (firstShotCount, secondShotCount) = _statistics[center];
				Console.WriteLine($"{secondShotCount}/{firstShotCount} {center}");
			}
		}
	}
}