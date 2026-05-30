using System.Configuration;
using System.Data;
using System.Windows;

namespace FinancyApplication
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Restore the user's last theme choice before any window opens
			// so they don't see a flash of light theme.
			bool wasDark = ThemeManager.LoadPreference();
			ThemeManager.ApplyTheme(wasDark);
		}
	}
}
