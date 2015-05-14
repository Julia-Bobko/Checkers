using System;
using System.Collections.Generic;
using System.Text;

namespace Checkers.Helpers
{
    public class SettingsHelper
    {
        private static Windows.Storage.ApplicationDataContainer RoamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
        public static int GetCurrentGamerId()
        {
            Object objIdGamer = RoamingSettings.Values["idFirstGamer"];
            if (objIdGamer == null)
            {
                return 0;
            }
            else
            {
                return (int)objIdGamer;
            }
        }
    }
}
