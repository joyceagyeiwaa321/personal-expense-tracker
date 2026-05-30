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

			ThemeManager.ApplyTheme();
		}
	}
}
