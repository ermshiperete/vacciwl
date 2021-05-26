using OpenQA.Selenium;

namespace VaccinationAppointmentScheduler
{
	public static class WebElementExtensions
	{
		public static bool IsDisabled(this IWebElement element)
		{
			return element.GetAttribute("disabled") != null;
		}
	}
}