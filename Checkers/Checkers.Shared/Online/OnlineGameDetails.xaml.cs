using Checkers.Entities;
using Checkers.Helpers;
using Checkers.Online;
using Checkers.Services;
using Facebook;
using Facebook.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml.Linq;
using VK.WindowsPhone.SDK;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Checkers
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OnlineGameDetails : Page
    {
        private GameService gameService = null;
        private ListBox ListCurrentGames = default(ListBox);
        private ListBox ListOnlineGamers = default(ListBox);
        private ListBox ListFinishedGames = default(ListBox);

        private TextBox FindLogin = default(TextBox);
        private DispatcherTimer UpdateStateTimer { get; set; }
        private int IdGame { get; set; }

        ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
        public OnlineGameDetails()
        {
            this.InitializeComponent();

#if WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#endif
            gameService = new GameService();
            CreateGame();

            UpdateStateTimer = new DispatcherTimer();
            UpdateStateTimer.Interval = TimeSpan.FromSeconds(5);
            UpdateStateTimer.Tick += async delegate
            {
                int myGamerId = SettingsHelper.GetCurrentGamerId();
                var response = await gameService.OnlineGameServiceCall("GET", String.Format("CheckInputGame/{0}", myGamerId));
                XDocument xml = XDocument.Parse(CleanXml(response));
                var result = xml.Root.Value;
                var d = result == "true" ? true : false;
                if (!result.Contains("00"))
                //if (!d)
                {
                    //прислать челу уведомление о входящей игре

                    ///////////////
                    CreateGame();//создаем для нас новую игру, чтобы остальные могли к нам подключиться
                    string idGame = xml.Descendants("CheckInputGameResult").FirstOrDefault().Element("IdGame").Value;
                    string idCurrentGamer = xml.Descendants("CheckInputGameResult").FirstOrDefault().Element("IdCurrentGamer").Value;
                    Frame.Navigate(typeof(OnlineGame), String.Format("{0},{1}", idCurrentGamer, idGame));
                }
            };
            UpdateStateTimer.Start();
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

        private async void StartGame(CheckersGame game)
        {
            var random = new Random();
            int currentValue = random.Next(1, 3);
            IdGame = game.IdGame;
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            int idSecondGamer = (int)roamingSettings.Values["idFirstGamer"];
            var idCurrentGamer = currentValue == 1 ? idSecondGamer : game.IdFirstGamer;
            var response = await gameService.OnlineGameServiceCall("GET", String.Format("StartGame/{0}/{1}/{2}", game.IdGame, idSecondGamer, idCurrentGamer));
            XDocument xml = XDocument.Parse(CleanXml(response));
            var result = xml.Root.Value;
            var isStarted = result == "true" ? true : false;
            if (!isStarted)
            {
                //TODO:
            }
            else
            {
                //roamingSettings.Values["idSecondGamer"] = game.IdFirstGamer;
                Frame.Navigate(typeof(OnlineGame), String.Format("{0},{1}", idCurrentGamer, game.IdGame));
            }
        }

        private void ListOnlineGamers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var game = (CheckersGame)e.AddedItems[0];
                StartGame(game);
            }
        }

        private void findLogin_TextChanged(object sender, TextChangedEventArgs e)
        {
            PopulateOnlineGamers(FindLogin.Text.Length > 0 ? FindLogin.Text : "0");
        }


        private async void CreateGame()
        {

            int idFirstGamer = (int)roamingSettings.Values["idFirstGamer"];
            // roamingSettings.Values.Remove("idFirstGamer");      
            var response = await gameService.OnlineGameServiceCall("GET", String.Format("CreateGame/{0}", idFirstGamer));
            XDocument xml = XDocument.Parse(response);
            var result = xml.Root.Value;
            var isCreated = result == "true" ? true : false;
            if (!isCreated)
            {

            }
        }



        private async void PopulateCurrentGames()
        {
            int idFirstGamer = (int)roamingSettings.Values["idFirstGamer"];
            string xmlListGames = await gameService.OnlineGameServiceCall("GET", String.Format("GetCurrentGames/{0}", idFirstGamer.ToString()));
            XDocument xml = XDocument.Parse(CleanXml(xmlListGames));
            var listGames = new List<CurrentGame>();
            foreach (var item in xml.Descendants("CurrentGame"))
            {
                try
                {
                    CurrentGame game = new CurrentGame();
                    game.IdGame = Convert.ToInt32(item.Element("IdGame").Value);
                    game.Login = string.IsNullOrEmpty(item.Element("Login").Value) ? item.Element("FirstName").Value + " " + item.Element("LastName").Value : item.Element("Login").Value;
                    game.FirstName = item.Element("FirstName").Value;
                    game.LastName = item.Element("LastName").Value;
                    game.ImageSource = item.Element("ImageSource").Value;
                    game.City = item.Element("City").Value;
                    game.Rating = Convert.ToInt32(item.Element("Rating").Value);
                    game.IdGamer = Convert.ToInt32(item.Element("IdGamer").Value);
                    listGames.Add(game);
                }
                catch (Exception)
                {

                }
            }
            ListCurrentGames.ItemsSource = listGames;
        }

        private async void PopulateOnlineGamers(string userName = "0")
        {
            int idFirstGamer = (int)roamingSettings.Values["idFirstGamer"];
            string xmlListGames = await gameService.OnlineGameServiceCall("GET", String.Format("GetGames/{0}/{1}/{2}", idFirstGamer.ToString(), userName, "0"));
            XDocument xml = XDocument.Parse(CleanXml(xmlListGames));
            var listGames = new List<CheckersGame>();
            foreach (var item in xml.Descendants("CheckersGame"))
            {
                try
                {
                    CheckersGame game = new CheckersGame();
                    game.IdGame = Convert.ToInt32(item.Element("IdGame").Value);
                    game.Login = string.IsNullOrEmpty(item.Element("Login").Value) ? item.Element("FirstName").Value + " " + item.Element("LastName").Value : item.Element("Login").Value;
                    game.FirstName = item.Element("FirstName").Value;
                    game.LastName = item.Element("LastName").Value;
                    game.ImageSource = item.Element("ImageSource").Value;
                    game.City = item.Element("City").Value;
                    game.Rating = Convert.ToInt32(item.Element("Rating").Value);
                    game.IdFirstGamer = Convert.ToInt32(item.Element("IdFirstGamer").Value);
                    listGames.Add(game);
                }
                catch (Exception)
                {

                }
            }
            ListOnlineGamers.ItemsSource = listGames;
        }

        private string CleanXml(string xml)
        {
            return xml
                .Replace("a:", "")
                .Replace("i:", "")
                .Replace("xmlns:a=\"http://schemas.datacontract.org/2004/07/GameService.Entities\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"", "")
                .Replace("http://tempuri.org/", "");
        }

        //private void findGame_Click(object sender, RoutedEventArgs e)
        //{
        //    Frame.Navigate(typeof(ListGamers));
        //}


        private void ListCurrentGames_Loaded(object sender, RoutedEventArgs e)
        {
            ListCurrentGames = sender as ListBox;
            PopulateCurrentGames();
        }

        private void ListOnlineGamers_Loaded(object sender, RoutedEventArgs e)
        {
            ListOnlineGamers = sender as ListBox;
            PopulateOnlineGamers();
        }

        private void ListCurrentGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var game = (CurrentGame)e.AddedItems[0];
                ResumeGame(game);
            }
        }

        private async void ResumeGame(CurrentGame game)
        {
            var response = await gameService.OnlineGameServiceCall("GET", String.Format("GetCurrentStatusGame/{0}", game.IdGame));
            XDocument xml = XDocument.Parse(CleanXml(response));
            string currentStatatus = xml.Descendants("GetCurrentStatusGameResult").FirstOrDefault().Element("CurrentStatus").Value;
            int idCurrentGamer = Convert.ToInt32(xml.Descendants("GetCurrentStatusGameResult").FirstOrDefault().Element("IdCurrentGamer").Value);
            int idGamerColorWhite = Convert.ToInt32(xml.Descendants("GetCurrentStatusGameResult").FirstOrDefault().Element("IdGamerColorWhite").Value);
            //roamingSettings.Values["idSecondGamer"] = game.IdGamer;
            Frame.Navigate(typeof(OnlineGame), String.Format("{0},{1},{2},{3}", idCurrentGamer, game.IdGame, currentStatatus, idGamerColorWhite));
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }

        private void findLogin_Loaded(object sender, RoutedEventArgs e)
        {
            FindLogin = sender as TextBox;
        }

        private void ListFinishedGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ListFinishedGames_Loaded(object sender, RoutedEventArgs e)
        {
            ListFinishedGames = sender as ListBox;
            PopulateFinishedGamers();
        }

        private async void PopulateFinishedGamers()
        {
            int idFirstGamer = (int)roamingSettings.Values["idFirstGamer"];
            string xmlListGames = await gameService.OnlineGameServiceCall("GET", String.Format("GetFinishedGames/{0}", idFirstGamer.ToString()));
            XDocument xml = XDocument.Parse(CleanXml(xmlListGames));
            var listGames = new List<CurrentGame>();
            foreach (var item in xml.Descendants("CurrentGame"))
            {
                try
                {
                    CurrentGame game = new CurrentGame();
                    game.IdGame = Convert.ToInt32(item.Element("IdGame").Value);
                    game.Login = string.IsNullOrEmpty(item.Element("Login").Value) ? item.Element("FirstName").Value + " " + item.Element("LastName").Value : item.Element("Login").Value;
                    game.FirstName = item.Element("FirstName").Value;
                    game.LastName = item.Element("LastName").Value;
                    game.ImageSource = item.Element("ImageSource").Value;
                    game.City = item.Element("City").Value;
                    game.Rating = Convert.ToInt32(item.Element("Rating").Value);
                    game.IdGamer = Convert.ToInt32(item.Element("IdGamer").Value);
                    listGames.Add(game);
                }
                catch (Exception)
                {

                }
            }
            ListFinishedGames.ItemsSource = listGames;
        }

        private async void logout_Tapped(object sender, TappedRoutedEventArgs e)
        {
            UpdateStateTimer.Stop();
            string authentication = roamingSettings.Values["authentication"].ToString();
            switch (authentication)
            {
                case "vk":
                    VKSDK.Logout();
                    break;
                case "fb":
                    {
                        Session.ActiveSession.Logout();
                        HttpBaseProtocolFilter myFilter = new HttpBaseProtocolFilter();
                        var cookieManager = myFilter.CookieManager;
                        HttpCookieCollection myCookieJar = cookieManager.GetCookies(new Uri("https://www.facebook.com"));
                        foreach (HttpCookie cookie in myCookieJar)
                        {
                            cookieManager.DeleteCookie(cookie);
                        }
                    }
                    break;
                default:
                    Frame.Navigate(typeof(AuthorizePage));
                    break;
            }
            roamingSettings.Values.Remove("idFirstGamer");
            roamingSettings.Values.Remove("authentication");
            Frame.Navigate(typeof(AuthorizePage));
        }
    }
}
