
using System.Configuration;

#nullable disable
namespace Timer.Properties
{
    internal sealed class Settings : ApplicationSettingsBase
    {
        private static Settings defaultInstance = (Settings)SettingsBase.Synchronized((SettingsBase)new Settings());

        public static Settings Default => Settings.defaultInstance;
        
        [UserScopedSetting]
        [DefaultSettingValue("25")]
        public int Minutes
        {
            get => (int)((SettingsBase)this)[nameof(Minutes)];
            set => ((SettingsBase)this)[nameof(Minutes)] = (object)value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("-1")]
        public double Left
        {
            get => (double)((SettingsBase)this)[nameof(Left)];
            set => ((SettingsBase)this)[nameof(Left)] = (object)value;
        }

        [UserScopedSetting]
        [DefaultSettingValue("-1")]
        public double Top
        {
            get => (double)((SettingsBase)this)[nameof(Top)];
            set => ((SettingsBase)this)[nameof(Top)] = (object)value;
        }
    }
}
