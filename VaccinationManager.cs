using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace VaccinationAppointmentScheduler
{
	public class VaccinationManager
	{
		private const    int           FirstShot  = 1;
		private const    int           SecondShot = 2;
		private          FirefoxDriver _driver;
		private readonly Options       _options;

		public VaccinationManager(Options options)
		{
			_options = options;
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
			_driver.WaitAndFindElement(By.XPath("//a[contains(.,'Nachschlagen mit Liste')]")).Click();
			_driver.FindElement(By.Id("s2id_autogen8_search")).SendKeys(vaccinationCenter);
			IWebElement result;
			for (result = _driver.WaitAndFindElement(By.ClassName("select2-result-label"));
				!result.Text.Contains(vaccinationCenter, StringComparison.OrdinalIgnoreCase);
				result = _driver.WaitAndFindElement(By.ClassName("select2-result-label")))
			{
				Thread.Sleep(100);
			}

			Console.WriteLine($"Checking {result.Text}:");
			result.Click();
		}

		private bool SelectFirstVaccinationCenter()
		{
			_driver.WaitAndFindElement(By.XPath("//a[contains(.,'Nachschlagen mit Liste')]")).Click();
			var result = _driver.WaitAndFindElement(By.ClassName("select2-result-label"));

			Console.WriteLine($"Checking {result.Text}:");
			result.Click();
			return true;
		}

		private bool SelectNextVaccinationCenter()
		{
			_driver.WaitAndFindElement(By.Id("s2id_sp_formfield_preferred_center")).Click();
			var previous = _driver.WaitAndFindElement(By.XPath("//li[contains(@class, 'select2-highlighted')]/div/div"));
			_driver.FindElement(By.Id("s2id_autogen8_search")).SendKeys(Keys.Down);
			var result = _driver.WaitAndFindElement(By.XPath("//li[contains(@class, 'select2-highlighted')]/div/div"));
			if (previous.Text == result.Text)
				return false;

			Console.WriteLine($"Checking {result.Text}:");
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
				Console.WriteLine("\tFinished. No slots on other days available.");
		}

		private bool BookSlotsIfAvailable()
		{
			var firstDivXPath = GetDivXPathForShot(FirstShot);
			var firstDate = GetSelectedDate(firstDivXPath);
			if (SelectNextAvailableSlot(FirstShot))
			{
				Console.WriteLine($"\tFound available slot on {firstDate}, looking for slots for second shot...");
				for (var success = SelectNextAvailableSecondSlot(firstDate);
					!success && SelectNextDay(GetDivXPathForShot(SecondShot));
					)
				{
					if (SelectNextAvailableSecondSlot(firstDate))
						return true;
				}
			}
			else
				Console.WriteLine($"\tNo slots on {firstDate}");

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
				if (_options.BookAppointment)
				{
					Console.WriteLine($"Congratulations! Reserved slots on {firstDate} and {secondDate}!");
					_driver.FindElement(By.XPath("//button[contains(.,'Absenden')]")).Click();
					return true;
				}
				Console.WriteLine($"Found available slots on {firstDate} and {secondDate}!");
				return false;
			}
			Console.WriteLine($"\t\tNo slots for second shot on {secondDate}");
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
	}
}