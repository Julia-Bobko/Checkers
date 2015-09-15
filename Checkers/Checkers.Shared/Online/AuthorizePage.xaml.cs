using Checkers.Entities;
using Checkers.Helpers;
using Checkers.Services;
using Facebook;
using Facebook.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Xml.Linq;
using VK.WindowsPhone.SDK;
using VK.WindowsPhone.SDK.API;
using VK.WindowsPhone.SDK.API.Model;
using VK.WindowsPhone.SDK.Util;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
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
        #region VK properties
        private List<String> _scope = new List<string> { VKScope.FRIENDS, VKScope.PHOTOS };
        #endregion

        #region FB properties

        private const string FBAppId = "831311033650537";
        const string QueryToGetFbInfo = "me";
        const string QueryToGetFbPhoto = "me?fields=picture.width(200).height(200)";
        const string QueryToGetFriendList = "me/friends?fields=name,picture.width(100).height(100)";

        #endregion

        private AuthorizeService authorizeService = null;
        public AuthorizePage()
        {
            this.InitializeComponent();

            #region VK инициализация
            VKSDK.Initialize("5067473");
            VKSDK.AccessTokenReceived += (sender, args) =>
            {
                VKUpdateUIState();
            };
            VKSDK.WakeUpSession();
            #endregion

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
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        private async void ok_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(login.Text) && !String.IsNullOrEmpty(password.Password))
            {
                string response = await authorizeService.AuthorizeServiceCall("GET", String.Format("CreateGamer/{0}/{1}/{2}", login.Text, "", MD5Helper.ComputeMD5(password.Password)));
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

        //private void cancel_Click(object sender, RoutedEventArgs e)
        //{
        //    Frame.Navigate(typeof(MainPage));
        //}

        #region VK
        private void vk_Tapped(object sender, TappedRoutedEventArgs e)
        {
            VKSDK.Authorize(_scope, false, false);
        }

        private void VKUpdateUIState()
        {
            try
            {
                bool isLoggedIn = VKSDK.IsLoggedIn;
                if (isLoggedIn)
                {
                    //progress.IsVisible = true;
                    VKGetUserInfo();
                }
                else
                {
                    //progress.IsVisible = false;
                    //var popup = new PopupMessage();
                    //popup.Show(AppResources.AuthorizationError);
                }
            }
            catch (Exception ex)
            {
                //var popup = new PopupMessage();
                //popup.Show(String.Format(AppResources.Exception, ex.Message));
            }
        }

        private void VKGetUserInfo()
        {
            try
            {
                VKUser user = null;
                VKRequest.Dispatch<List<VKUser>>(
                    new VKRequestParameters(
                        "users.get",
                        "fields", "photo_200, city, country"),
                    (res) =>
                    {
                        if (res.ResultCode == VKResultCode.Succeeded)
                        {
                            VKExecute.ExecuteOnUIThread(() =>
                            {
                                user = res.Data[0];
                                userInfo.Text = user.first_name + " " + user.last_name;
                                SaveSettings("vk", user.id, user.photo_200, user.first_name, user.last_name, user.city != null && !String.IsNullOrEmpty(user.city.title) ? user.city.title : "Unknown");
                            });
                        }
                        else
                        {
                            var dialog = new MessageDialog("Увы", "Не вышло:(");
                        }
                    });
            }
            catch (Exception ex)
            {
                //var popup = new PopupMessage();
                //popup.Show(String.Format(AppResources.Exception, ex.Message));
            }
        }

        #endregion

        #region FB
        private void fb_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Session.OnFacebookAuthenticationFinished += OnFacebookAuthenticationFinished;
            Session.ActiveSession.LoginWithBehavior("email,public_profile,user_friends", FacebookLoginBehavior.LoginBehaviorWebViewOnly);
        }

        private async void OnFacebookAuthenticationFinished(AccessTokenData session)
        {
            if (session != null && !string.IsNullOrEmpty(session.AccessToken))
            {
                var fb = new FacebookClient(session.AccessToken);
                dynamic result = await fb.GetTaskAsync("me?fields=picture.width(100).height(100),first_name,last_name,location");
                var user = new GraphUser(result);
                SaveSettings("fb", long.Parse(user.Id), user.ProfilePictureUrl.AbsoluteUri, user.FirstName, user.LastName, user.Location != null && !String.IsNullOrEmpty(user.Location.City) ? user.Location.City : "Unknown");
            }
        }
        #endregion

        private async void SaveSettings(string authentication, long id, string photo_200, string first_name, string last_name, string city)
        {
            var gamer = new Gamer
            {
                Authentication = authentication,
                SocialId = id,
                ImageSource = photo_200,
                FirstName = first_name,
                LastName = last_name,
                City = city,
                Email = string.Empty,
                Login = string.Empty,
                HashPassword = string.Empty
            };
            var serialized = JsonConvert.SerializeObject(new { obj = gamer });
            var response = await authorizeService.AuthorizeServiceCall("POST", "CreateGamer", new StringContent(serialized, Encoding.UTF8, "application/json"));
            XDocument xml = XDocument.Parse(response);
            int idFirstGamer = Convert.ToInt32(xml.Root.Value);
            if (idFirstGamer <= 0)
            {
                return;//TODO:
            }
            else
            {
                var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                roamingSettings.Values["idFirstGamer"] = idFirstGamer;
                roamingSettings.Values["authentication"] = authentication;
                Frame.Navigate(typeof(OnlineGameDetails));
            }
        }
    }
}
