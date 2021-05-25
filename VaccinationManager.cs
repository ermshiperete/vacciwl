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
		private FirefoxDriver _driver;

		public void Main(string user, string password, string impfZentrum)
		{
			_driver = new FirefoxDriver(".") { Url = "https://itm-wl.service-now.com/vam" };
			try
			{
				Login(user, password);
				SelectImpfzentrum(impfZentrum);

				_driver.WaitForElement(By.ClassName("appointmentContentContainer"));
				if (SelectFirstAvailableSlot())
				{
					if (SelectFirstAvailableSlot(By.XPath("//div[contains(@class, 'dosage') and position() = 2]")))
						Console.WriteLine("Found slots!");
					else
						Console.WriteLine("No slots for second shot");
				}
				else
					Console.WriteLine("No slots");
			}
			finally
			{
				_driver.Close();
			}
		}

		private bool SelectFirstAvailableSlot(By by = null)
		{
			var slotName = By.ClassName("appointmentSlot");
			IReadOnlyCollection<IWebElement> availableSlots;
			if (by == null)
				availableSlots = _driver.FindElements(slotName);
			else
			{
				var secondShotDiv = _driver.WaitAndFindElement(by);
				availableSlots = secondShotDiv.FindElements(slotName);
			}

			if (availableSlots.Count <= 0)
				return false;

			availableSlots.First().Click();
			return true;
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