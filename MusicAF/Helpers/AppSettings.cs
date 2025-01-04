using Windows.Storage;

namespace MusicAF.Helpers
{
    internal class AppSettings
    {
        public static void SaveSetting(string key, object value)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }

        public static T GetSetting<T>(string key, T defaultValue = default)
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out object value))
            {
                return (T)value;
            }
            return defaultValue;
        }
    }
}
