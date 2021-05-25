// Copyright (c) 2021 Eberhard Beilharz
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace Impfterminportal
{
	class Program
	{
		static void Main(string[] args)
		{
			var driver = new FirefoxDriver(".") { Url = "https://itm-wl.service-now.com/vam" };
			try
			{
				if (driver.FindElements(By.CssSelector(".sub-avatar")).Count == 0)
				{
					driver.FindElement(By.XPath("//button[contains(.,'Anmelden')]")).Click();
					driver.FindElement(By.Id("username"))
						.SendKeys(Environment.GetEnvironmentVariable("ITM_USER"));
					driver.FindElement(By.Id("password"))
						.SendKeys(Environment.GetEnvironmentVariable("ITM_PW"));
					driver.FindElement(By.XPath("//button[contains(.,'Anmelden')]")).Click();
				}

				driver.WaitAndFindElement(By.XPath("//button[contains(.,'Termin buchen')]")).Click();
				driver.WaitAndFindElement(By.XPath("//a[contains(.,'Nachschlagen mit Liste')]")).Click();
				var impfZentrum = "siegen-wittgenstein";
				driver.FindElement(By.Id("s2id_autogen8_search")).SendKeys(impfZentrum);
				IWebElement result;
				for (result = driver.WaitAndFindElement(By.ClassName("select2-result-label"));
					!result.Text.Contains(impfZentrum, StringComparison.OrdinalIgnoreCase);
					result = driver.WaitAndFindElement(By.ClassName("select2-result-label")))
				{
					Thread.Sleep(100);
				}

				result.Click();
			}
			finally
			{
				driver.Close();
			}
		}
	}
}
