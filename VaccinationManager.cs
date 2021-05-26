using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace Impfterminportal
{
	public class VaccinationManager
	{
		private       FirefoxDriver _driver;
		private const int           FirstShot  = 1;
		private const int           SecondShot = 2;

		public void Main(string user, string password, string impfZentrum)
		{
			_driver = new FirefoxDriver(".") { Url = "https://itm-wl.service-now.com/vam" };
			try
			{
				Login(user, password);
				SelectImpfzentrum(impfZentrum);

				_driver.WaitForElement(By.ClassName("appointmentContentContainer"));
				for (var success = BookSlotsIfAvailable();
					!success && SelectNextDay();
					)
				{
					success = BookSlotsIfAvailable();
				}
			}
			finally
			{
				_driver.Close();
			}
		}

		private bool BookSlotsIfAvailable()
		{
			var firstDivXPath = GetDivXPathForShot(FirstShot);
			var firstDate = GetSelectedDate(firstDivXPath);
			if (SelectNextAvailableSlot(FirstShot))
			{
				Console.Write($"Found available slot on {firstDate}, looking for slots for second shot... ");
				var secondDivXPath = GetDivXPathForShot(SecondShot);
				var secondDate = GetSelectedDate(secondDivXPath);
				if (SelectNextAvailableSlot(SecondShot))
				{
					Console.WriteLine($"Found slots on {firstDate} and {secondDate}!");
					return true;
				}
				Console.WriteLine($"No slots for second shot on {secondDate}");
			}
			else
				Console.WriteLine($"No slots on {firstDate}");

			return false;
		}

		private bool SelectNextDay()
		{
			static bool SelectNextDayInRow(IReadOnlyCollection<IWebElement> readOnlyCollection)
			{
				if (readOnlyCollection.Count <= 0 || readOnlyCollection.First().IsDisabled())
					return false;

				readOnlyCollection.First().Click();
				Thread.Sleep(500);
				return true;
			}

			var nextDays = _driver.FindElements(By.XPath("//td[contains(@class, 'selectedDate')]/following-sibling::td/button"));
			if (SelectNextDayInRow(nextDays))
				return true;

			nextDays = _driver.FindElements(By.XPath(
				"//td[contains(@class, 'selectedDate')]/parent::tr/following-sibling::tr/td/button"));
			if (SelectNextDayInRow(nextDays))
				return true;

			// at end of month
			var goNext = _driver.FindElement(By.Id("goNext"));
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

		private void SelectImpfzentrum(string impfZentrum)
		{
			_driver.WaitAndFindElement(By.XPath("//button[contains(.,'Termin buchen')]")).Click();
			_driver.WaitAndFindElement(By.XPath("//a[contains(.,'Nachschlagen mit Liste')]")).Click();
			_driver.FindElement(By.Id("s2id_autogen8_search")).SendKeys(impfZentrum);
			IWebElement result;
			for (result = _driver.WaitAndFindElement(By.ClassName("select2-result-label"));
				!result.Text.Contains(impfZentrum, StringComparison.OrdinalIgnoreCase);
				result = _driver.WaitAndFindElement(By.ClassName("select2-result-label")))
			{
				Thread.Sleep(100);
			}

			result.Click();
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
	}
}