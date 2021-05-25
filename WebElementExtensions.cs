using OpenQA.Selenium;

namespace Impfterminportal
{
	public static class WebElementExtensions
	{
		public static bool IsDisabled(this IWebElement element)
		{
			return element.GetAttribute("disabled") != null;
		}
	}
}