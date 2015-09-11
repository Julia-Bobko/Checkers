using Checkers.Entities;
using Checkers.Helpers;
using Checkers.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Checkers
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OnlineGame : Page
    {
        public Grid SelectedChecker { get; set; }
        public CheckerColors CurrentCheckers { get; set; }
        public Grid LastKillCheckerWhite { get; set; }
        public Grid LastKillCheckerBlack { get; set; }
        public Grid LastKillDCheckerWhite { get; set; }
        public Grid LastKillDCheckerBlack { get; set; }

        private int IdCurrentGamer { get; set; }
        private int IdCurrentGame { get; set; }

        private bool IsResetForCheckers = true;
        private bool IsResetForDCheckers = true;
        private DispatcherTimer UpdateStateTimer { get; set; }
        private XDocument LastMovedCheckers { get; set; }
        private XDocument LastDeletedCheckers { get; set; }
        public XDocument CurrentStatus { get; set; }
        public int IdGamerColorWhite { get; set; }
        private GameService gameService = null;
        public OnlineGame()
        {
            this.InitializeComponent();
            gameService = new GameService();
            SetGameGridProperties();
            //PopulateCheckers();
            // LastDeletedCheckers = null;
            //LastMovedCheckers = null;
            //CurrentCheckers = CheckerColors.White;
            UpdateStateTimer = new DispatcherTimer();
            UpdateStateTimer.Interval = TimeSpan.FromSeconds(3);

#if WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#endif

            UpdateStateTimer.Tick += async delegate
            {
                int myGamerId = SettingsHelper.GetCurrentGamerId();
                var response = await gameService.OnlineGameServiceCall("GET", String.Format("GetStatusGame/{0}/{1}", IdCurrentGame, myGamerId));
                XDocument xml = XDocument.Parse(response);
                var result = xml.Root.Value;
                if (!String.IsNullOrEmpty(result))
                {
                    UpdateStateTimer.Stop();
                    IdCurrentGamer = myGamerId;
                    SetCurrentStep();
                    //распарсить xml и обновить доску                   
                    XDocument xmlresponse = XDocument.Parse(CleanXml(response));
                    string lastMovedCheckers = xmlresponse.Descendants("GetStatusGameResult").FirstOrDefault().Element("LastMovedCheckers").Value;
                    XDocument xmlLastMovedCheckers = XDocument.Parse(lastMovedCheckers);
                    // foreach (var item in xmlLastMovedCheckers.Descendants("step"))
                    foreach (var item in xmlLastMovedCheckers.Descendants("root").Elements("step"))
                    {
                        try
                        {
                            string before = (item.Attribute("before").Value).ToString();
                            GridChecker gridCheckerBefore = new GridChecker()
                            {
                                Column = Convert.ToByte(before[0].ToString()),
                                Row = Convert.ToByte(before[1].ToString())
                            };
                            string after = (item.Attribute("after").Value).ToString();
                            GridChecker gridCheckerAfter = new GridChecker()
                            {
                                Column = Convert.ToByte(after[0].ToString()),
                                Row = Convert.ToByte(after[1].ToString())
                            };
                            MoveChecker(gridCheckerBefore, gridCheckerAfter, false);
                        }
                        catch (Exception)
                        {

                        }
                    }

                    string lastDeletedCheckers = xmlresponse.Descendants("GetStatusGameResult").FirstOrDefault().Element("LastDeletedCheckers").Value;
                    if (!string.IsNullOrEmpty(lastDeletedCheckers))
                    {
                        XDocument xmlLastDeletedCheckers = XDocument.Parse(lastDeletedCheckers);
                        foreach (var item in xmlLastDeletedCheckers.Descendants("checker"))
                        {
                            try
                            {
                                string checker = (item.Value).ToString();
                                GridChecker gridChecker = new GridChecker()
                                {
                                    Column = Convert.ToByte(checker[0].ToString()),
                                    Row = Convert.ToByte(checker[1].ToString())
                                };
                                DeleteChecker(gridChecker, false);
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }

                    string winnerGamer = xmlresponse.Descendants("GetStatusGameResult").FirstOrDefault().Element("IdWinner").Value;
                    if (winnerGamer != "")
                    {
                        var dialog = new MessageDialog("Поражение", "Вы проиграли(");
                        dialog.Commands.Add(new UICommand("Ok", new UICommandInvokedHandler(CommandHandlers)));
                        await dialog.ShowAsync();
                    }
                }
            };
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
       

        private string CleanXml(string xml)
        {
            return xml
                .Replace("a:", "")
                .Replace("i:", "")
                .Replace("xmlns:a=\"http://schemas.datacontract.org/2004/07/GameService.Entities\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"", "")
                .Replace("http://tempuri.org/", "");
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string[] parameters = e.Parameter.ToString().Split(',');
            IdCurrentGamer = Convert.ToInt32(parameters[0]);
            IdCurrentGame = Convert.ToInt32(parameters[1]);
           
            if (parameters.Length > 2)
            {
                CurrentStatus = XDocument.Parse(parameters[2]);
                IdGamerColorWhite = Convert.ToInt32(parameters[3]);
                if (IdGamerColorWhite == SettingsHelper.GetCurrentGamerId())
                {
                    CurrentCheckers = CheckerColors.White;                  
                }
                else
                { 
                    CurrentCheckers = CheckerColors.Black;
                    GridRotation();
                }
            }
            //установить в настройках цвет моих шашек в данной игре 
            else if (IdCurrentGamer == SettingsHelper.GetCurrentGamerId())
            {
                CurrentCheckers = CheckerColors.White;
            }
            else
            {
                CurrentCheckers = CheckerColors.Black;
                GridRotation();
            }
            SetCurrentStep();
            PopulateCheckers();
            UpdateStateTimer.Start();
        }

        public void GridRotation() 
        {
            gameGridPlane.RotationZ = 180;
        }

        private void SetCurrentStep(bool isRotateGrid = false)
        {
            int myGamerId = SettingsHelper.GetCurrentGamerId();
            if (IsMyStep())
            {
                currentGamerStep.Visibility = Visibility.Visible;
                remoteGamerStep.Visibility = Visibility.Collapsed;
            }
            else
            {
                //if (isRotateGrid) gameGridPlane.RotationZ = 180;
                currentGamerStep.Visibility = Visibility.Collapsed;
                remoteGamerStep.Visibility = Visibility.Visible;
            }
        }

        private bool IsMyStep()
        {
            return IdCurrentGamer == SettingsHelper.GetCurrentGamerId() ? true : false;
        }

        private void SetGameGridProperties()
        {
            var bounds = Window.Current.Bounds;
            double height = bounds.Height;
            double width = bounds.Width;
            height = height / 12 * 10;
            width = width / 12 * 10;
            if (width > height)
            {
                gameGrid.Height = height;
                gameGrid.Width = height;
            }
            else
            {
                gameGrid.Width = width;
                gameGrid.Height = width;
            }
        }

        public bool IscompleteGame()
        {
            int countCheckerBlack = 0, countCheckerWhite = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Grid cellBoard;

                    if (!(i % 2 == 0 && j % 2 == 0) || !(i % 2 != 0 && j % 2 != 0))
                    {
                        cellBoard = (Grid)FindName(String.Format("f{0}{1}", i, j));
                        Image checker = (Image)cellBoard.Children.LastOrDefault();
                        if (checker != null)
                        {
                            if (checker.Tag.ToString() == "b")
                            {
                                countCheckerBlack++;
                            }
                            else
                            {
                                countCheckerWhite++;
                            }
                        }
                        if (countCheckerBlack >= 1 && countCheckerWhite >= 1)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;

        }

        private void PopulateCheckers()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Grid cellBoard;

                    if ((i % 2 == 0 && j % 2 == 0) || (i % 2 != 0 && j % 2 != 0))
                    {
                        cellBoard = (Grid)FindName(String.Format("f{0}{1}", i, j));
                        cellBoard.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri("ms-appx:///Images/whiteBoard.png")) };
                    }
                    else
                    {
                        cellBoard = (Grid)FindName(String.Format("f{0}{1}", i, j));
                        cellBoard.Background = new ImageBrush { ImageSource = new BitmapImage(new Uri("ms-appx:///Images/blackBoard.png")) };
                    }
                    if (CurrentStatus == null)
                    {
                        //if (((i % 2 == 0 && j % 2 == 0) || (i % 2 != 0 && j % 2 != 0)) && (j < 3 || j > 4))
                        if (((i % 2 == 0 && j % 2 != 0) || (i % 2 != 0 && j % 2 == 0)) && (j < 3 || j > 4))
                        {
                            Image checker = new Image
                            {
                                //Source = new BitmapImage(new Uri(j < 3 ? "ms-appx:///Images/checker-black.png" : "ms-appx:///Images/checker-white.png")),
                                Source = new BitmapImage(new Uri(j < 3 ? "ms-appx:///Images/blackChecker.png" : "ms-appx:///Images/redChecker.png")),

#if WINDOWS_PHONE_APP
                                Margin = new Thickness(5),
#else
                                Margin = new Thickness(10),
#endif
                                Tag = j < 3 ? "b" : "w"
                            };
                            // var field = (Grid)FindName(String.Format("f{0}{1}", i, j));
                            cellBoard.Children.Add(checker);
                        }
                    }
                }
            }
            if (CurrentStatus != null)
            {
                MatrixToGrid(CurrentStatus);
            }
        }


        private void MatrixToGrid(XDocument CurrentStatus)
        {
            foreach (var item in CurrentStatus.Descendants("matrix").Elements())
            {
                try
                {
                    string checkerColor = (item.Value).ToString();
                    string checkerImage = "";
                    if (checkerColor != "")
                    {
                        Grid cellBoard = (Grid)FindName(item.Name.ToString());
                        if (checkerColor == "w")
                        {
                            checkerImage = "ms-appx:///Images/redChecker.png";
                        }
                        else
                        {
                            checkerImage = "ms-appx:///Images/blackChecker.png";
                        }
                        Image checker = new Image
                                       {
                                           Source = new BitmapImage(new Uri(checkerImage)),
#if WINDOWS_PHONE_APP
                                           Margin = new Thickness(5),
#else
                                           Margin = new Thickness(10),
#endif
                                           Tag = checkerColor
                                       };
                        cellBoard.Children.Add(checker);
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        //private void MatrixToGrid()
        //{
        //    for (int i = 0; i < 8; i++)
        //    {
        //        for (int j = 0; j < 8; j++)
        //        {
        //            //if (((i % 2 == 0 && j % 2 == 0) || (i % 2 != 0 && j % 2 != 0)))
        //            if (((i % 2 == 0 && j % 2 != 0) || (i % 2 != 0 && j % 2 == 0)))
        //            {
        //                //Grid grid = (Grid)FindName(String.Format("f{0}{1}", i, j));
        //                //if (grid.Children.Count != 0)
        //                //{
        //                //    grid.Children.Clear();
        //                //    Image checker = new Image
        //                //    {
        //                //        Source = new BitmapImage(new Uri(matrix[i, j] < "b" ? "ms-appx:///Images/checker-black.png" : "ms-appx:///Images/checker-white.png")),
        //                //        Tag = j < 3 ? "b" : "w"
        //                //    };
        //                //    grid.Children.Add(checker);
        //                //    var checker = (Image)grid.Children.FirstOrDefault();
        //                //    matrix[i, j] = checker.Tag.ToString();
        //                //}
        //                //else
        //                //{
        //                //    matrix[i, j] = String.Empty;
        //                //}
        //            }
        //        }
        //    }
        //}


        private void f_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (IsMyStep())
            {
                Grid currentSender = (Grid)sender;
                if (currentSender.Children.Count != 0)//клик по шашке
                {
                    SelectedChecker = currentSender;
                }
                else if (currentSender.Children.Count == 0 && SelectedChecker != null)//ход шашкой
                {
                    var selectedChecker = currentSender;
                    //чтобы не нажимать дважды на шашку, которой бьешь, если бьешь не одну шашку за раз
                    if (LastKillCheckerBlack != null)
                    {
                        SelectedChecker = LastKillCheckerBlack;
                    }
                    else if (LastKillCheckerWhite != null)
                    {
                        SelectedChecker = LastKillCheckerWhite;
                    }
                    else if (LastKillDCheckerBlack != null)
                    {
                        SelectedChecker = LastKillDCheckerBlack;
                    }
                    else if (LastKillDCheckerWhite != null)
                    {
                        SelectedChecker = LastKillDCheckerWhite;
                    }
                    GoStep(SelectedChecker, selectedChecker);
                    //чтобы не нажимать дважды на шашку, которой бьешь, если бьешь не одну шашку за раз
                    if (LastKillCheckerBlack == null && LastKillCheckerWhite == null && LastKillDCheckerBlack == null && LastKillDCheckerWhite == null)
                    {
                        SelectedChecker = null;
                    }
                }
            }
        }

        private void GoStep(Grid currentGrid, Grid newGrid)
        {
            /////////////////
            var newImg = (Image)currentGrid.Children.FirstOrDefault();
            if ((CurrentCheckers == CheckerColors.White && newImg.Tag.ToString().Contains("b")) || ((CurrentCheckers == CheckerColors.Black && newImg.Tag.ToString().Contains("w"))) || (LastKillCheckerWhite != null && currentGrid != LastKillCheckerWhite) || (LastKillCheckerBlack != null && currentGrid != LastKillCheckerBlack) || (LastKillDCheckerWhite != null && currentGrid != LastKillDCheckerWhite) || (LastKillDCheckerBlack != null && currentGrid != LastKillDCheckerBlack))
            {
                return;
            }
            ///////////////////////
            var currentGridChecker = new GridChecker
            {
                Column = Convert.ToByte(currentGrid.Name[1].ToString()),
                Row = Convert.ToByte(currentGrid.Name[2].ToString())
            };
            var newGridChecker = new GridChecker
            {
                Column = Convert.ToByte(newGrid.Name[1].ToString()),
                Row = Convert.ToByte(newGrid.Name[2].ToString())
            };
            if (IsBlackGrid(newGridChecker))//если черное поле, а не белое
            {
                if (IsChecker(currentGrid))//если шашка, а не дамка
                {
                    if (NeedKillForCheckers(CurrentCheckers).Count == 0 && NeedKillForDCheckers(CurrentCheckers).Count == 0)
                    {
                        if (IsOneField(currentGridChecker, newGridChecker))
                        {
                            //MoveChecker(currentGrid, newGrid);
                            MoveChecker(currentGridChecker, newGridChecker);
                            ResetSteps();
                        }
                    }
                    else
                    {
                        var isKill = OneFieldKill(currentGridChecker, newGridChecker);
                        PopulateListMovedCheckers(currentGridChecker, newGridChecker);
                        #region для обязательного битья неограниченного кол-ва шашек подряд
                        IsResetForCheckers = true;
                        IsResetForDCheckers = true;
                        var listCombination = NeedKillForCheckers(CurrentCheckers);
                        if (CurrentCheckers == CheckerColors.White && LastKillCheckerWhite != null)
                        {
                            foreach (var list in listCombination)
                            {
                                var grid = (Grid)FindName(String.Format("f{0}{1}", list[0].Column, list[0].Row));
                                if (grid == LastKillCheckerWhite)
                                {
                                    IsResetForCheckers = false;
                                    break;
                                }
                            }
                            //если при битье шашка переходит в дамку, то проверяем нужно ли этой дамке сразу бить
                            if (IsResetForCheckers)
                            {
                                var listDCombination = NeedKillForDCheckers(CurrentCheckers);
                                foreach (var list in listDCombination)
                                {
                                    var grid = (Grid)FindName(String.Format("f{0}{1}", list[0].Column, list[0].Row));
                                    if (grid == LastKillCheckerWhite)
                                    {
                                        IsResetForCheckers = false;
                                        break;
                                    }
                                }
                            }
                        }
                        else if (CurrentCheckers == CheckerColors.Black && LastKillCheckerBlack != null)
                        {
                            foreach (var list in listCombination)
                            {
                                var grid = (Grid)FindName(String.Format("f{0}{1}", list[0].Column, list[0].Row));
                                if (grid == LastKillCheckerBlack)
                                {
                                    IsResetForCheckers = false;
                                    break;
                                }
                            }
                            //если при битье шашка переходит в дамку, то проверяем нужно ли этой дамке сразу бить
                            if (IsResetForCheckers)
                            {
                                var listDCombination = NeedKillForDCheckers(CurrentCheckers);
                                foreach (var list in listDCombination)
                                {
                                    var grid = (Grid)FindName(String.Format("f{0}{1}", list[0].Column, list[0].Row));
                                    if (grid == LastKillCheckerBlack)
                                    {
                                        IsResetForCheckers = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (IsResetForCheckers && isKill)
                        {
                            CheckAvailabaleSendGridToServer();
                            ResetSteps();
                            LastKillCheckerBlack = null;
                            LastKillCheckerWhite = null;
                        }
                        #endregion
                    }

                }
                else if (IsDamka(currentGrid))//если дамка
                {
                    if (NeedKillForCheckers(CurrentCheckers).Count == 0 && NeedKillForDCheckers(CurrentCheckers).Count == 0)
                    {
                        GoDamka(currentGridChecker, newGridChecker);
                    }
                    else
                    {
                        GoDamka(currentGridChecker, newGridChecker, true);
                    }
                }
            }
        }

        private void ResetSteps()
        {
            //if (CurrentCheckers == CheckerColors.White)
            //{
            //    CurrentCheckers = CheckerColors.Black;
            //}
            //else
            //{
            //    CurrentCheckers = CheckerColors.White;
            //}
        }


        //private async void MoveChecker(Grid currentGrid, Grid newGrid)
        //{
        //    var newImg = (Image)currentGrid.Children.FirstOrDefault();
        //    if (IsChecker(currentGrid) && IsNextDamka(currentGrid, newGrid))
        //    {
        //        if (newImg.Tag.ToString() == "w")
        //        {
        //            newImg.Tag = "dw";
        //            newImg.Source = new BitmapImage(new Uri("ms-appx:///Images/dchecker-white.png"));
        //        }
        //        else if (newImg.Tag.ToString() == "b")
        //        {
        //            newImg.Tag = "db";
        //            newImg.Source = new BitmapImage(new Uri("ms-appx:///Images/dchecker-black.png"));
        //        }
        //    }
        //    currentGrid.Children.Clear();
        //    newGrid.Children.Add(newImg);
        //    if (IsResetForCheckers || IsResetForDCheckers)
        //    {
        //        //обновить доску и передать ход другому
        //        var isSent = await SendGridToServer();
        //    }
        //    else
        //    {
        //        LastMovedCheckers.Root.Add(new XElement("step", new XAttribute("before", String.Format("{0}{1}",currentGrid.))))
        //    }

        //}

        private void MoveChecker(GridChecker currentGridChecker, GridChecker newGridChecker, bool isWatchForMoves = true)
        {
            Grid currentGrid = (Grid)FindName(String.Format("f{0}{1}", currentGridChecker.Column, currentGridChecker.Row));
            Grid newGrid = (Grid)FindName(String.Format("f{0}{1}", newGridChecker.Column, newGridChecker.Row));
            var newImg = (Image)currentGrid.Children.FirstOrDefault();
            if (IsChecker(currentGrid) && IsNextDamka(currentGrid, newGrid))
            {
                if (newImg.Tag.ToString() == "w")
                {
                    newImg.Tag = "dw";
                    newImg.Source = new BitmapImage(new Uri("ms-appx:///Images/dRedChecker.png"));
                }
                else if (newImg.Tag.ToString() == "b")
                {
                    newImg.Tag = "db";
                    newImg.Source = new BitmapImage(new Uri("ms-appx:///Images/dBlackChecker.png"));
                }
            }
            DeleteChecker(currentGrid, false);
            newGrid.Children.Add(newImg);

            if (isWatchForMoves)
            {
                PopulateListMovedCheckers(currentGridChecker, newGridChecker);

                CheckAvailabaleSendGridToServer();
            }
        }

        private async void CheckAvailabaleSendGridToServer()
        {
            if (IsResetForCheckers && IsResetForDCheckers)
            {
                //обновить доску и передать ход другому
                var isSent = await SendGridToServer();
                if (isSent)
                {
                    if (IscompleteGame())
                    {
                        var response = await gameService.OnlineGameServiceCall("GET", String.Format("FinishGame/{0}/{1}", IdCurrentGame, IdCurrentGamer));
                        XDocument xml = XDocument.Parse(response);
                        var result = xml.Root.Value == "true" ? true : false;
                        if (result)
                        {
                            if (IdCurrentGamer == SettingsHelper.GetCurrentGamerId())
                            {
                                var dialog = new MessageDialog("Победа", "Вы выйграли");
                                dialog.Commands.Add(new UICommand("Ok", new UICommandInvokedHandler(CommandHandlers)));
                                // dialog.Commands.Add(new UICommand("Quit", new UICommandInvokedHandler(CommandHandlers)));
                                await dialog.ShowAsync();
                            }
                            //else
                            //{
                            //    var dialog = new MessageDialog("Поражение", "Вы проиграли");
                            //    dialog.Commands.Add(new UICommand("Ok", new UICommandInvokedHandler(CommandHandlers)));
                            //    // dialog.Commands.Add(new UICommand("Quit", new UICommandInvokedHandler(CommandHandlers)));
                            //    await dialog.ShowAsync();
                            //}

                        }
                        //уведомление 
                    }
                    else
                    {
                        UpdateStateTimer.Start();
                        IdCurrentGamer = -1;
                        SetCurrentStep();
                    }
                }
                LastMovedCheckers = null;
                LastDeletedCheckers = null;
            }
        }

        public void CommandHandlers(IUICommand commandLabel)
        {
            var Actions = commandLabel.Label;
            switch (Actions)
            {
                case "Ok":
                    Frame.Navigate(typeof(OnlineGameDetails));
                    break;
                case "Quit":
                    Application.Current.Exit();
                    break;
            }
        }

        private void PopulateListMovedCheckers(GridChecker currentGridChecker, GridChecker newGridChecker)
        {
            if (LastMovedCheckers == null)
            {
                LastMovedCheckers = new XDocument(new XElement("root"));
            }
            LastMovedCheckers.Root.Add(new XElement("step",
                                                   new XAttribute("before", String.Format("{0}{1}", currentGridChecker.Column, currentGridChecker.Row)),
                                                   new XAttribute("after", String.Format("{0}{1}", newGridChecker.Column, newGridChecker.Row))
                                                  )
                                     );
        }

        private void PopulateListDeletedCheckers(GridChecker deletedGridChecker)
        {
            if (LastDeletedCheckers == null)
            {
                LastDeletedCheckers = new XDocument(new XElement("root"));
            }
            LastDeletedCheckers.Root.Add(new XElement("checker", String.Format("{0}{1}", deletedGridChecker.Column, deletedGridChecker.Row)));
        }

        private void DeleteChecker(Grid grid, bool isWatchForDeletes = true)
        {
            grid.Children.Clear();
            var gridChecker = new GridChecker
            {
                Column = Convert.ToByte(grid.Name[1].ToString()),
                Row = Convert.ToByte(grid.Name[2].ToString())
            };
            if (isWatchForDeletes)
            {
                PopulateListDeletedCheckers(gridChecker);
            }
        }

        private void DeleteChecker(GridChecker gridChecker, bool isWatchForDeletes = true)
        {
            Grid grid = (Grid)FindName(String.Format("f{0}{1}", gridChecker.Column, gridChecker.Row));
            grid.Children.Clear();
            if (isWatchForDeletes)
            {
                PopulateListDeletedCheckers(gridChecker);
            }
        }

        private string[,] GridToMatrix()
        {
            string[,] matrix = new string[8, 8];
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    //if (((i % 2 == 0 && j % 2 == 0) || (i % 2 != 0 && j % 2 != 0)))
                    if (((i % 2 == 0 && j % 2 != 0) || (i % 2 != 0 && j % 2 == 0)))
                    {
                        Grid grid = (Grid)FindName(String.Format("f{0}{1}", i, j));
                        if (grid.Children.Count != 0)
                        {
                            var checker = (Image)grid.Children.FirstOrDefault();
                            matrix[i, j] = checker.Tag.ToString();
                        }
                        else
                        {
                            matrix[i, j] = String.Empty;
                        }
                    }
                }
            }
            return matrix;
        }


        private async Task<bool> SendGridToServer()
        {

            string[,] matrix = GridToMatrix();
            XDocument xmlMatrix = new XDocument(new XElement("matrix"));
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (((i % 2 == 0 && j % 2 != 0) || (i % 2 != 0 && j % 2 == 0)))
                    {
                        xmlMatrix.Root.Add(new XElement(String.Format("f{0}{1}", i, j), matrix[i, j]));
                    }
                }
            }

            //byte[] utfBytes = Encoding.UTF8.GetBytes(xmlMatrix.ToString());
            //string str = "";
            //for (int i = 0; i < utfBytes.Length; i++)
            //{
            //    if (i != utfBytes.Length - 1) { str += utfBytes[i].ToString() + ","; }
            //    else { str += utfBytes[i].ToString(); }
            //}
            //  string myStr = Encoding.UTF8.GetString(utfBytes, 0, utfBytes.Length);

            //string str = xmlMatrix.ToString().Replace("<", "a").Replace(">", "c").Replace("\r\n", "").Replace("/", "b").Replace(" ", "");
            // Encoding e1 = Encoding.GetEncoding(xmlMatrix.ToString());

            //var response = await gameService.OnlineGameServiceCall("GET", String.Format("UpdateStatusGame/{0}/{1}/{2}", IdCurrentGame, System.Net.WebUtility.UrlEncode(str), IdCurrentGamer));
            var updateStatus = new UpdateStatus()
            {
                IdGame = IdCurrentGame,
                XmlCurrentStatus = xmlMatrix.ToString(),
                IdGamer = IdCurrentGamer,
                LastMovedCheckers = LastMovedCheckers.ToString(),
                LastDeletedCheckers = LastDeletedCheckers == null ? null : LastDeletedCheckers.ToString()
            };
            var serialized = JsonConvert.SerializeObject(new { obj = updateStatus });
            var response = await gameService.OnlineGameServiceCall("POST", "UpdateStatusGame", new StringContent(serialized, Encoding.UTF8, "application/json"));
            XDocument xml = XDocument.Parse(response);
            bool result = Convert.ToBoolean(xml.Root.Value);
            return result;
        }

        private bool OneFieldKill(GridChecker currentGridChecker, GridChecker newGridChecker)
        {
            var matrix = GridToMatrix();
            string color = matrix[currentGridChecker.Column, currentGridChecker.Row] == "w" ? "b" : "w";
            Grid lastChecker = null;
            //северо-восток
            if (newGridChecker.Column - currentGridChecker.Column == 2 && newGridChecker.Row - currentGridChecker.Row == -2 && matrix[currentGridChecker.Column + 1, currentGridChecker.Row - 1].Contains(color))
            {
                var grid = (Grid)FindName(String.Format("f{0}{1}", currentGridChecker.Column + 1, currentGridChecker.Row - 1));
                DeleteChecker(grid);
                MoveChecker(currentGridChecker, newGridChecker, false);
                lastChecker = (Grid)FindName(String.Format("f{0}{1}", newGridChecker.Column, newGridChecker.Row));
            }
            //северо-запад
            else if (newGridChecker.Column - currentGridChecker.Column == -2 && newGridChecker.Row - currentGridChecker.Row == -2 && matrix[currentGridChecker.Column - 1, currentGridChecker.Row - 1].Contains(color))
            {
                var grid = (Grid)FindName(String.Format("f{0}{1}", currentGridChecker.Column - 1, currentGridChecker.Row - 1));
                DeleteChecker(grid);
                MoveChecker(currentGridChecker, newGridChecker, false);
                lastChecker = (Grid)FindName(String.Format("f{0}{1}", newGridChecker.Column, newGridChecker.Row));
            }
            //юго-запад
            else if (newGridChecker.Column - currentGridChecker.Column == -2 && newGridChecker.Row - currentGridChecker.Row == 2 && matrix[currentGridChecker.Column - 1, currentGridChecker.Row + 1].Contains(color))
            {
                var grid = (Grid)FindName(String.Format("f{0}{1}", currentGridChecker.Column - 1, currentGridChecker.Row + 1));
                DeleteChecker(grid);
                MoveChecker(currentGridChecker, newGridChecker, false);
                lastChecker = (Grid)FindName(String.Format("f{0}{1}", newGridChecker.Column, newGridChecker.Row));
            }
            //юго-восток
            else if (newGridChecker.Column - currentGridChecker.Column == 2 && newGridChecker.Row - currentGridChecker.Row == 2 && matrix[currentGridChecker.Column + 1, currentGridChecker.Row + 1].Contains(color))
            {
                var grid = (Grid)FindName(String.Format("f{0}{1}", currentGridChecker.Column + 1, currentGridChecker.Row + 1));
                DeleteChecker(grid);
                MoveChecker(currentGridChecker, newGridChecker, false);
                lastChecker = (Grid)FindName(String.Format("f{0}{1}", newGridChecker.Column, newGridChecker.Row));
            }
            if (lastChecker != null && matrix[currentGridChecker.Column, currentGridChecker.Row] == "w")
            {
                LastKillCheckerWhite = lastChecker;
                return true;
            }
            else if (lastChecker != null && matrix[currentGridChecker.Column, currentGridChecker.Row] == "b")
            {
                LastKillCheckerBlack = lastChecker;
                return true;
            }
            return false;

        }

        //private bool IsNextDamka(GridChecker gridChecker)
        //{
        //    Grid grid = (Grid)FindName(String.Format("f{0}{1}", gridChecker.Column, gridChecker.Row));
        //    var checker = (Image)grid.Children.FirstOrDefault();
        //    if (checker.Tag.ToString() == "w" && gridChecker.Row == 0)
        //    {
        //        return true;
        //    }
        //    else if (checker.Tag.ToString() == "b" && gridChecker.Row == 7)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        private bool IsNextDamka(Grid currentGrid, Grid grid)
        {
            var checker = (Image)currentGrid.Children.FirstOrDefault();
            if (checker.Tag.ToString() == "w" && grid.Name[2].ToString() == "0")
            {
                return true;
            }
            else if (checker.Tag.ToString() == "b" && grid.Name[2].ToString() == "7")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsChecker(Grid currentGrid)
        {
            var checker = (Image)currentGrid.Children.FirstOrDefault();
            if (checker.Tag.ToString() == "w" || checker.Tag.ToString() == "b")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsDamka(Grid currentGrid)
        {
            var checker = (Image)currentGrid.Children.FirstOrDefault();
            if (checker.Tag.ToString() == "dw" || checker.Tag.ToString() == "db")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsBlackGrid(GridChecker newGridChecker)
        {
            var matrix = GridToMatrix();
            return matrix[newGridChecker.Column, newGridChecker.Row] != null ? true : false;
        }

        private bool IsOneField(GridChecker currentGridChecker, GridChecker newGridChecker)
        {
            var matrix = GridToMatrix();
            if (matrix[currentGridChecker.Column, currentGridChecker.Row] == "w" || matrix[currentGridChecker.Column, currentGridChecker.Row] == "dw")
            {
                return Math.Abs(currentGridChecker.Column - newGridChecker.Column) == 1 && (currentGridChecker.Row - newGridChecker.Row == 1) ? true : false;
            }
            else
            {
                return Math.Abs(currentGridChecker.Column - newGridChecker.Column) == 1 && (newGridChecker.Row - currentGridChecker.Row == 1) ? true : false;
            }
        }

        public void GoDamka(GridChecker currentGridChecker, GridChecker newGridChecker, bool isNeedKill = false)
        {
            var matrix = GridToMatrix();
            string currentColor = matrix[currentGridChecker.Column, currentGridChecker.Row];
            string colorAlien = currentColor.Contains("w") ? "b" : "w";
            int len = Math.Abs(currentGridChecker.Column - newGridChecker.Column);//расстояние между дамкой и ее новой позицией
            List<GridChecker> list = new List<GridChecker>();
            if (currentGridChecker.Column - newGridChecker.Column == currentGridChecker.Row - newGridChecker.Row)//если дамка и новая позиция на обратной диагонали 
            {
                for (byte i = 1; i < len; i++)//проверяем есть ли шашки/дамки между дамкой и новой позицией
                {
                    string item = currentGridChecker.Column - newGridChecker.Column > 0 ? matrix[newGridChecker.Column + i, newGridChecker.Row + i] : matrix[currentGridChecker.Column + i, currentGridChecker.Row + i];
                    if (item != String.Empty && item.Contains(colorAlien))
                    {
                        list.Add(new GridChecker
                        {
                            Column = Convert.ToByte(currentGridChecker.Column - newGridChecker.Column > 0 ? newGridChecker.Column + i : currentGridChecker.Column + i),
                            Row = Convert.ToByte(currentGridChecker.Column - newGridChecker.Column > 0 ? newGridChecker.Row + i : currentGridChecker.Row + i)
                        });
                    }
                    else if (item != String.Empty && !item.Contains(colorAlien))//если на этом промежутке есть своя шашка/дамка 
                    {
                        return;//то ничего не делаем
                    }
                }
                if (list.Count == 0 && !isNeedKill)//если препятсвий нет между ними и необязательно бить
                {
                    MoveChecker(currentGridChecker, newGridChecker);//то ходим дамкой
                    ResetSteps();//и передаем ход противнику
                }
                else if (list.Count == 1)//если есть одно препятствие(шашка/дамка)
                {
                    DeleteChecker(list[0]);//убираем препятствие
                    MoveChecker(currentGridChecker, newGridChecker, false);//то ходим дамкой         
                    PopulateListMovedCheckers(currentGridChecker, newGridChecker);
                    //сохраняем последнюю дамку, которой били
                    if (matrix[currentGridChecker.Column, currentGridChecker.Row] == "dw")
                    {
                        LastKillDCheckerWhite = (Grid)FindName(String.Format("f{0}{1}", newGridChecker.Column, newGridChecker.Row));
                    }
                    else if (matrix[currentGridChecker.Column, currentGridChecker.Row] == "db")
                    {
                        LastKillDCheckerBlack = (Grid)FindName(String.Format("f{0}{1}", newGridChecker.Column, newGridChecker.Row));
                    }
                    LastKillCheckerBlack = null;
                    LastKillCheckerWhite = null;
                    //проверяем можно ли этой дамке еще бить
                    #region для обязательного битья неограниченного кол-ва шашек подряд
                    IsResetForCheckers = true;
                    IsResetForDCheckers = true;
                    var listCombination = NeedKillForDCheckers(CurrentCheckers);
                    if (CurrentCheckers == CheckerColors.White && LastKillDCheckerWhite != null)
                    {
                        foreach (var listComb in listCombination)
                        {
                            var grid = (Grid)FindName(String.Format("f{0}{1}", listComb[0].Column, listComb[0].Row));
                            if (grid == LastKillDCheckerWhite)
                            {
                                IsResetForDCheckers = false;
                                break;
                            }
                        }
                    }
                    else if (CurrentCheckers == CheckerColors.Black && LastKillDCheckerBlack != null)
                    {
                        foreach (var listComb in listCombination)
                        {
                            var grid = (Grid)FindName(String.Format("f{0}{1}", listComb[0].Column, listComb[0].Row));
                            if (grid == LastKillDCheckerBlack)
                            {
                                IsResetForDCheckers = false;
                                break;
                            }
                        }
                    }
                    if (IsResetForDCheckers)
                    {
                        CheckAvailabaleSendGridToServer();
                        ResetSteps();
                        LastKillDCheckerBlack = null;
                        LastKillDCheckerWhite = null;
                    }
                    #endregion
                }
            }
            else if ((-1) * (currentGridChecker.Column - newGridChecker.Column) == currentGridChecker.Row - newGridChecker.Row)//если дамка и жертва на прямой диагонали 
            {
                for (int i = 1; i < len; i++)//проверяем есть ли шашки/дамки между дамкой и новой позицией
                {
                    string item = currentGridChecker.Column - newGridChecker.Column > 0 ? matrix[newGridChecker.Column + i, newGridChecker.Row - i] : matrix[currentGridChecker.Column + i, currentGridChecker.Row - i];
                    if (item != String.Empty && item.Contains(colorAlien))
                    {
                        list.Add(new GridChecker
                        {
                            Column = Convert.ToByte(currentGridChecker.Column - newGridChecker.Column > 0 ? newGridChecker.Column + i : currentGridChecker.Column + i),
                            Row = Convert.ToByte(currentGridChecker.Column - newGridChecker.Column > 0 ? newGridChecker.Row - i : currentGridChecker.Row - i)
                        });
                    }
                    else if (item != String.Empty && !item.Contains(colorAlien))//если на этом промежутке есть своя шашка/дамка
                    {
                        return;//то ничего не делаем
                    }
                }
                if (list.Count == 0 && !isNeedKill)//если препятсвий нет между ними и необязательно бить
                {
                    MoveChecker(currentGridChecker, newGridChecker);//то ходим дамкой
                    ResetSteps();//и передаем ход противнику
                }
                else if (list.Count == 1)//если есть одно препятствие(шашка/дамка)
                {
                    DeleteChecker(list[0]);//убираем препятствие
                    MoveChecker(currentGridChecker, newGridChecker);//то ходим дамкой                
                    PopulateListMovedCheckers(currentGridChecker, newGridChecker);
                    //сохраняем последнюю дамку, которой били
                    if (matrix[currentGridChecker.Column, currentGridChecker.Row] == "dw")
                    {
                        LastKillDCheckerWhite = (Grid)FindName(String.Format("f{0}{1}", newGridChecker.Column, newGridChecker.Row));
                    }
                    else if (matrix[currentGridChecker.Column, currentGridChecker.Row] == "db")
                    {
                        LastKillDCheckerBlack = (Grid)FindName(String.Format("f{0}{1}", newGridChecker.Column, newGridChecker.Row));
                    }
                    LastKillCheckerBlack = null;
                    LastKillCheckerWhite = null;
                    //проверяем можно ли этой дамке еще бить
                    #region для обязательного битья неограниченного кол-ва шашек подряд
                    IsResetForCheckers = true;
                    IsResetForDCheckers = true;
                    var listCombination = NeedKillForDCheckers(CurrentCheckers);
                    if (CurrentCheckers == CheckerColors.White && LastKillDCheckerWhite != null)
                    {
                        foreach (var listComb in listCombination)
                        {
                            var grid = (Grid)FindName(String.Format("f{0}{1}", listComb[0].Column, listComb[0].Row));
                            if (grid == LastKillDCheckerWhite)
                            {
                                IsResetForDCheckers = false;
                                break;
                            }
                        }
                    }
                    else if (CurrentCheckers == CheckerColors.Black && LastKillDCheckerBlack != null)
                    {
                        foreach (var listComb in listCombination)
                        {
                            var grid = (Grid)FindName(String.Format("f{0}{1}", listComb[0].Column, listComb[0].Row));
                            if (grid == LastKillDCheckerBlack)
                            {
                                IsResetForDCheckers = false;
                                break;
                            }
                        }
                    }
                    if (IsResetForDCheckers)
                    {
                        CheckAvailabaleSendGridToServer();
                        ResetSteps();
                        LastKillDCheckerBlack = null;
                        LastKillDCheckerWhite = null;
                    }
                    #endregion
                }
            }
        }

        public List<List<GridChecker>> NeedKillForCheckers(CheckerColors checkers)//проверяем есть ли шашки, которым нужно бить в данный момент
        {
            var listCombination = new List<List<GridChecker>>();
            var matrix = GridToMatrix();
            var color = checkers == CheckerColors.White ? "w" : "b";
            var colorAlien = checkers == CheckerColors.White ? "b" : "w";
            for (byte i = 0; i < 8; i++)
            {
                for (byte j = 0; j < 8; j++)
                {
                    if (matrix[i, j] != null && matrix[i, j] == color)
                    {
                        //северо-восток
                        if (i + 2 <= 7 && j - 2 >= 0 && matrix[i + 1, j - 1].Contains(colorAlien) && matrix[i + 2, j - 2] == String.Empty)
                        {
                            listCombination.Add(
                                new List<GridChecker>
                                {
                                    new GridChecker
                                    {
                                        Column = i,
                                        Row = j
                                    },
                                    new GridChecker
                                    {
                                        Column = Convert.ToByte(i + 2),
                                        Row = Convert.ToByte(j - 2)
                                    }
                                });
                        }
                        //северо-запад
                        else if (i - 2 >= 0 && j - 2 >= 0 && matrix[i - 1, j - 1].Contains(colorAlien) && matrix[i - 2, j - 2] == String.Empty)
                        {
                            listCombination.Add(
                                new List<GridChecker>
                                {
                                    new GridChecker
                                    {
                                        Column = i,
                                        Row = j
                                    },
                                    new GridChecker
                                    {
                                        Column = Convert.ToByte(i - 2),
                                        Row = Convert.ToByte(j - 2)
                                    }
                                });
                        }
                        //юго-запад
                        if (i - 2 >= 0 && j + 2 <= 7 && matrix[i - 1, j + 1].Contains(colorAlien) && matrix[i - 2, j + 2] == String.Empty)
                        {
                            listCombination.Add(
                                new List<GridChecker>
                                {
                                    new GridChecker
                                    {
                                        Column = i,
                                        Row = j
                                    },
                                    new GridChecker
                                    {
                                        Column = Convert.ToByte(i - 2),
                                        Row = Convert.ToByte(j + 2)
                                    }
                                });
                        }
                        //юго-восток
                        else if (i + 2 <= 7 && j + 2 <= 7 && matrix[i + 1, j + 1].Contains(colorAlien) && matrix[i + 2, j + 2] == String.Empty)
                        {
                            listCombination.Add(
                                new List<GridChecker>
                                {
                                    new GridChecker
                                    {
                                        Column = i,
                                        Row = j
                                    },
                                    new GridChecker
                                    {
                                        Column = Convert.ToByte(i + 2),
                                        Row = Convert.ToByte(j + 2)
                                    }
                                });
                        }
                    }
                }
            }
            return listCombination;
        }

        public List<List<GridChecker>> NeedKillForDCheckers(CheckerColors checkers)//проверяем есть ли дамки, которым нужно бить в данный момент
        {
            var listCombination = new List<List<GridChecker>>();
            var matrix = GridToMatrix();
            var color = checkers == CheckerColors.White ? "dw" : "db";//наш цвет
            var colorAlien = checkers == CheckerColors.White ? "b" : "w";//цвет противника
            var listDCheckers = new List<GridChecker>();
            for (byte i = 0; i < 8; i++)//находим все дамки и сохраняем в список
            {
                for (byte j = 0; j < 8; j++)
                {
                    if (matrix[i, j] != null && matrix[i, j].Contains(color))
                    {
                        listDCheckers.Add(new GridChecker
                        {
                            Column = i,
                            Row = j
                        });
                    }
                }
            }
            if (listDCheckers.Count == 0) return listCombination;//если наших дамок нет, то ничего не делаем
            foreach (var item in listDCheckers)//проходим по каждой дамке
            {
                for (byte i = 0; i < 8; i++)
                {
                    for (byte j = 0; j < 8; j++)
                    {
                        if (item.Column - i == item.Row - j && matrix[i, j] != null && matrix[i, j].Contains(colorAlien) && i != 0 && i != 7 && j != 0 && j != 7)//исключаем крайние элементы и свои + выбираем расположенные на обраной диагонали
                        {
                            //if (item.Column > i)//дамка ниже шашки, которую нужно бить
                            {
                                //if (item.Column < item.Row)//ниже диагонали
                                {
                                    bool isExistCheckers = false;
                                    for (int k = 1; ((item.Column > i ? i : item.Column) + k) < (item.Column > i ? item.Column : i); k++)//проходим промежуток от дамки до шашки
                                    {
                                        if (matrix[(item.Column > i ? i : item.Column) + k, (item.Column > i ? j : item.Row) + k] != String.Empty)
                                        {
                                            isExistCheckers = true;
                                            break;
                                        }
                                    }
                                    if (!isExistCheckers)
                                    {
                                        for (int k = 1; (item.Column > i ? i - k >= 0 && j - k >= 0 : i + k <= 7 && j + k <= 7); k++)//проходим промежуток от шашки до конца
                                        {
                                            if (matrix[(item.Column > i ? i - k : i + k), (item.Column > i ? j - k : j + k)] == String.Empty)
                                            {
                                                listCombination.Add(
                                                    new List<GridChecker>
                                                        {
                                                            new GridChecker
                                                            {
                                                                Column = item.Column,
                                                                Row = item.Row
                                                            },
                                                            new GridChecker
                                                            {
                                                                Column = Convert.ToByte((item.Column > i ? i - k: i + k)),
                                                                Row = Convert.ToByte((item.Column > i ? j - k : j + k))
                                                            }
                                                        }
                                                );
                                                break;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                        }
                        else if (((-1) * (item.Column - i) == item.Row - j) && matrix[i, j] != null && matrix[i, j].Contains(colorAlien) && i != 0 && i != 7 && j != 0 && j != 7)//исключаем крайние элементы и свои + выбираем расположенные на прямой диагонали
                        {
                            bool isExistCheckers = false;
                            for (int k = 1; ((item.Column > i ? i : item.Column) + k) < (item.Column > i ? item.Column : i); k++)//проходим промежуток от дамки до шашки
                            {
                                if (matrix[(item.Column > i ? i : item.Column) + k, (item.Column > i ? j : item.Row) - k] != String.Empty)
                                {
                                    isExistCheckers = true;
                                    break;
                                }
                            }
                            if (!isExistCheckers)
                            {
                                for (int k = 1; (item.Column > i ? i - k >= 0 && j - k >= 0 : i + k <= 7 && j + k <= 7); k++)//проходим промежуток от шашки до конца
                                {
                                    if (matrix[(item.Column > i ? i - k : i + k), (item.Column > i ? j + k : j - k)] == String.Empty)
                                    {
                                        listCombination.Add(
                                            new List<GridChecker>
                                                        {
                                                            new GridChecker
                                                            {
                                                                Column = item.Column,
                                                                Row = item.Row
                                                            },
                                                            new GridChecker
                                                            {
                                                                Column = Convert.ToByte((item.Column > i ? i - k: i + k)),
                                                                Row = Convert.ToByte((item.Column > i ? j + k : j - k))
                                                            }
                                                        }
                                        );
                                        break;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return listCombination;
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
