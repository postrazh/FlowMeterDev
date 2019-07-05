// Helpers/Settings.cs This file was automatically added when you installed the Settings Plugin. If you are not using a PCL then comment this file back in to use it.
using Plugin.Settings;
using Plugin.Settings.Abstractions;

namespace FlowMeter.Helpers
{
	/// <summary>
	/// This is the Settings static class that can be used in your Core solution or in any
	/// of your client applications. All settings are laid out the same exact way with getters
	/// and setters. 
	/// </summary>
	public static class Settings
	{
		private static ISettings AppSettings
		{
			get
			{
				return CrossSettings.Current;
			}
		}

        #region Setting Constants

        private const string Asset01NameDefault = "Asset101";
        private const string Asset02NameDefault = "Asset112";
        private const string Asset03NameDefault = "Asset136";
        private const string Asset04NameDefault = "Asset124";
        private const string Asset05NameDefault = "P5a";
        private const string Asset06NameDefault = "P6a";
        private const string Asset07NameDefault = "1157";

        private const double Asset01ValueDefault = 0.473;
        private const double Asset02ValueDefault = 9.181;
        private const double Asset03ValueDefault = 27.651;
        private const double Asset04ValueDefault = 18.434;
        private const double Asset05ValueDefault = 1.23;
        private const double Asset06ValueDefault = 1.19;
        private const double Asset07ValueDefault = 1.20;

        #endregion

        // asset names
        public static string Asset01Name
        {
			get => AppSettings.GetValueOrDefault(nameof(Asset01Name), Asset01NameDefault);
			set => AppSettings.AddOrUpdateValue(nameof(Asset01Name), value);
		}

        public static string Asset02Name
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset02Name), Asset02NameDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset02Name), value);
        }

        public static string Asset03Name
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset03Name), Asset03NameDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset03Name), value);
        }

        public static string Asset04Name
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset04Name), Asset04NameDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset04Name), value);
        }

        public static string Asset05Name
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset05Name), Asset05NameDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset05Name), value);
        }

        public static string Asset06Name
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset06Name), Asset06NameDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset06Name), value);
        }

        public static string Asset07Name
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset07Name), Asset07NameDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset07Name), value);
        }

        // asset values
        public static double Asset01Value
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset01Value), Asset01ValueDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset01Value), value);
        }
        public static double Asset02Value
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset02Value), Asset02ValueDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset02Value), value);
        }
        public static double Asset03Value
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset03Value), Asset03ValueDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset03Value), value);
        }
        public static double Asset04Value
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset04Value), Asset04ValueDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset04Value), value);
        }
        public static double Asset05Value
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset05Value), Asset05ValueDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset05Value), value);
        }
        public static double Asset06Value
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset06Value), Asset06ValueDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset06Value), value);
        }
        public static double Asset07Value
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset07Value), Asset07ValueDefault);
            set => AppSettings.AddOrUpdateValue(nameof(Asset07Value), value);
        }

        // toggle state
        public static bool Asset01Toggle
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset01Toggle), true);
            set => AppSettings.AddOrUpdateValue(nameof(Asset01Toggle), value);
        }
        public static bool Asset02Toggle
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset02Toggle), true);
            set => AppSettings.AddOrUpdateValue(nameof(Asset02Toggle), value);
        }
        public static bool Asset03Toggle
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset03Toggle), true);
            set => AppSettings.AddOrUpdateValue(nameof(Asset03Toggle), value);
        }
        public static bool Asset04Toggle
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset04Toggle), true);
            set => AppSettings.AddOrUpdateValue(nameof(Asset04Toggle), value);
        }
        public static bool Asset05Toggle
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset05Toggle), true);
            set => AppSettings.AddOrUpdateValue(nameof(Asset05Toggle), value);
        }
        public static bool Asset06Toggle
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset06Toggle), false);
            set => AppSettings.AddOrUpdateValue(nameof(Asset06Toggle), value);
        }
        public static bool Asset07Toggle
        {
            get => AppSettings.GetValueOrDefault(nameof(Asset07Toggle), false);
            set => AppSettings.AddOrUpdateValue(nameof(Asset07Toggle), value);
        }
        public static bool AssetExtraToggle
        {
            get => AppSettings.GetValueOrDefault(nameof(AssetExtraToggle), false);
            set => AppSettings.AddOrUpdateValue(nameof(AssetExtraToggle), value);
        }

        // extra value
        public static string AssetExtraValue
        {
            get => AppSettings.GetValueOrDefault(nameof(AssetExtraValue), "0");
            set => AppSettings.AddOrUpdateValue(nameof(AssetExtraValue), value);
        }
    }
}