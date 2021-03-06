// Copyright (c) 2021 Eberhard Beilharz
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)

using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace VaccinationAppointmentScheduler
{
	public static class DriverExtensions
	{
		public static IWebElement WaitAndFindElement(this FirefoxDriver driver, By by)
		{
			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
			return wait.Until(d => d.FindElement(by));
		}

		public static bool WaitForElement(this FirefoxDriver driver, By by)
		{
			var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
			return wait.Until(d => d.FindElement(by)) != null;
		}
	}
}
