using System;
using System.IO;
using System.Windows;

namespace FinancyApplication
{
	public partial class ConsentDialog : Window
	{
		public bool Accepted { get; private set; }

		public ConsentDialog()
		{
			InitializeComponent();
		}

		private void Accept_Click(object sender, RoutedEventArgs e)
		{
			Accepted = true;
			SaveConsent();
			DialogResult = true;
			Close();
		}

		private void Decline_Click(object sender, RoutedEventArgs e)
		{
			Accepted = false;
			DialogResult = false;
			Close();
		}


		private static string ConsentPath
		{
			get
			{
				string dir = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					"FinancyApp");
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
				return Path.Combine(dir, "consented.txt");
			}
		}

		public static bool HasConsented()
		{
			try { return File.Exists(ConsentPath); }
			catch { return false; }
		}

		private static void SaveConsent()
		{
			try { File.WriteAllText(ConsentPath, DateTime.UtcNow.ToString("o")); }
			catch { /* non-fatal — they'll be asked again next launch */ }
		}
	}
}