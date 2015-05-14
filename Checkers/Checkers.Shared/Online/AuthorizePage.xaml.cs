using Checkers.Helpers;
using Checkers.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Checkers.Online
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AuthorizePage : Page
    {
        private AuthorizeService authorizeService = null;
        public AuthorizePage()
        {
            this.InitializeComponent();
            authorizeService = new AuthorizeService();

#if WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#endif

        }

#if WINDOWS_PHONE_APP
        private void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            Frame frame = Window.Current.Content as Frame;
            if (frame == null)
            {
                return;
            }

            if (frame.CanGoBack)
            {
                frame.GoBack();
                e.Handled = true;
            }
        }
#endif 

        private async void ok_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(login.Text) && !String.IsNullOrEmpty(email.Text) && !String.IsNullOrEmpty(password.Password))
            {
                string response = await authorizeService.AuthorizeServiceCall("GET", String.Format("CreateGamer/{0}/{1}/{2}", login.Text, email.Text, MD5Helper.ComputeMD5(password.Password)));
                XDocument xml = XDocument.Parse(response);
                var result = xml.Root.Value;
                int idFirstGamer = result != "-1" ? Convert.ToInt32(result) : Convert.ToInt32(result);
                if (idFirstGamer == -1)
                {
                    return;//TODO:
                }
                else
                {
                    var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                    roamingSettings.Values["idFirstGamer"] = idFirstGamer;
                    Frame.Navigate(typeof(OnlineGameDetails));
                }
            }
            else
            {
                //TODO:
            }
        }

        private void cancel_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
    }
}
