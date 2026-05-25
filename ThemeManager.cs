using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace FinancyApplication
{
    public static class ThemeManager
    {
        private const string LightUri = "/Themes/LightTheme.xaml";
        private const string DarkUri  = "/Themes/DarkTheme.xaml";

        public static bool IsDarkMode { get; private set; }

        public static event EventHandler ThemeChanged;

        public static void ApplyTheme(bool dark)
        {
            IsDarkMode = dark;

            Application app = Application.Current;
            if (app == null) return;

            ResourceDictionary newDict = new ResourceDictionary
            {
                Source = new Uri(dark ? DarkUri : LightUri, UriKind.Relative)
            };

            Collection<ResourceDictionary> merged = app.Resources.MergedDictionaries;
            int existingIndex = -1;
            for (int i = 0; i < merged.Count; i++)
            {
                string src = merged[i].Source != null
                    ? merged[i].Source.OriginalString
                    : string.Empty;
                if (src.EndsWith("LightTheme.xaml", StringComparison.OrdinalIgnoreCase) ||
                    src.EndsWith("DarkTheme.xaml",  StringComparison.OrdinalIgnoreCase))
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                merged[existingIndex] = newDict;
            }
            else
            {
                merged.Add(newDict);
            }

            SavePreference(dark);

            if (ThemeChanged != null)
            {
                ThemeChanged(null, EventArgs.Empty);
            }
        }

        public static void Toggle()
        {
            ApplyTheme(!IsDarkMode);
        }

        private static string PrefPath
        {
            get
            {
                string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dir = System.IO.Path.Combine(folder, "FinancyApp");
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                return System.IO.Path.Combine(dir, "theme.txt");
            }
        }

        public static bool LoadPreference()
        {
            try
            {
                if (System.IO.File.Exists(PrefPath))
                {
                    string val = System.IO.File.ReadAllText(PrefPath).Trim();
                    return val == "dark";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        private static void SavePreference(bool dark)
        {
            try
            {
                System.IO.File.WriteAllText(PrefPath, dark ? "dark" : "light");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
