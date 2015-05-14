using Checkers.Entities;
using Checkers.Helpers;
using Checkers.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

namespace Checkers
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OnlineGameDetails : Page
    {
        private GameService gameService = null;
        private ListBox ListCurrentGames = default(ListBox);
        public OnlineGameDetails()
        {
            this.InitializeComponent();

#if WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#endif
            gameService = new GameService();
            CreateGame();
            //PopulateCurrentGames();
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

        private async void CreateGame()
        {
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
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
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
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
                    game.Login = item.Element("Login").Value;
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

        private string CleanXml(string xml)
        {
            return xml
                .Replace("a:", "")
                .Replace("i:", "")
                .Replace("xmlns:a=\"http://schemas.datacontract.org/2004/07/GameService.Entities\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"", "")
                .Replace("http://tempuri.org/", "");
        }

        private void findGame_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ListGamers));
        }


        private void ListCurrentGames_Loaded(object sender, RoutedEventArgs e)
        {
            // var a = (ListBox)sender;
            ListCurrentGames = sender as ListBox;
            PopulateCurrentGames();            
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
            Frame.Navigate(typeof(OnlineGame), String.Format("{0},{1},{2},{3}", idCurrentGamer, game.IdGame, currentStatatus, idGamerColorWhite));
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
