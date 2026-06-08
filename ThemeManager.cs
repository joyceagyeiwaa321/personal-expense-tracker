using System.Collections.ObjectModel;
using System.Windows;

namespace FinancyApplication
{
    public static class ThemeManager
    {
        private const string LightUri = "/Themes/LightTheme.xaml";

        public static void ApplyTheme()
        {
            Application app = Application.Current;
            if (app == null) return;

            ResourceDictionary newDict = new ResourceDictionary
            {
                Source = new System.Uri(LightUri, System.UriKind.Relative)
            };

            Collection<ResourceDictionary> merged = app.Resources.MergedDictionaries;
            for (int i = 0; i < merged.Count; i++)
            {
                string src = merged[i].Source != null ? merged[i].Source.OriginalString : string.Empty;
                if (src.EndsWith("LightTheme.xaml", System.StringComparison.OrdinalIgnoreCase))
                {
                    merged[i] = newDict;
                    return;
                }
            }

            merged.Add(newDict);
        }
    }
}
