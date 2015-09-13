using Checkers.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Checkers
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LocalGame : Page
    {
        public Grid SelectedChecker { get; set; }
        public CheckerColors CurrentCheckers { get; set; }
        public Grid LastKillCheckerWhite { get; set; }
        public Grid LastKillCheckerBlack { get; set; }
        public Grid LastKillDCheckerWhite { get; set; }
        public Grid LastKillDCheckerBlack { get; set; }

        private int MaxZIndex = 1000;
        private SynchronizationContext Sync { get; set; }

        public LocalGame()
        {
            this.InitializeComponent();
            SetGameGridProperties();
            PopulateCheckers();
            CurrentCheckers = CheckerColors.White;

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
       
        //private void SetGridCanvasProperties()
        //{
        //    double offset = gameGrid.Width / 8;
        //    for (int i = 0; i < 8; i++)
        //    {
        //        for (int j = 0; j < 8; j++)
        //        {
        //            Grid cellBoard;
        //            cellBoard = (Grid)FindName(String.Format("f{0}{1}", i, j));
        //            cellBoard.Margin = new Thickness(offset * i, offset * j, 0, 0);
        //            cellBoard.Height = cellBoard.Width = offset;
        //        }
        //    }
        //}

        private void SetGameGridProperties()
        {
            //var dialog = new MessageDialog(result);
            //await dialog.ShowAsync();

            var bounds = Window.Current.Bounds;
            double height = bounds.Height;
            double width = bounds.Width;
            height = height / 12 * 10;
            width = width / 12 * 10;
            if (width > height)
            {
                gameGrid.Height = height;
                gameGrid.Width = height;
                //gameCanvas.Height = gameGrid.Height;
                //gameCanvas.Width = gameGrid.Width;
            }
            else
            {
                gameGrid.Width = width;
                gameGrid.Height = width;
                //gameCanvas.Height = gameGrid.Height;
                //gameCanvas.Width = gameGrid.Width;
            }
            //SetGridCanvasProperties();
        }

        private void PopulateCheckers()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Grid cellBoard;
                    //if (((i % 2 == 0 && j % 2 == 0) || (i % 2 != 0 && j % 2 != 0)) && (j < 3 || j > 4))
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

                    if (((i % 2 == 0 && j % 2 != 0) || (i % 2 != 0 && j % 2 == 0)) && (j < 3 || j > 4))
                    {
                        Image checker = new Image
                        {
                            //Source = new BitmapImage(new Uri(j < 3 ? "ms-appx:///Images/checker-black.png" : "ms-appx:///Images/checker-white.png")),
                            Source = new BitmapImage(new Uri(j < 3 ? "ms-appx:///Images/blackChecker.png" : "ms-appx:///Images/redChecker.png")),
#if WINDOWS_PHONE_APP
                            Margin = new Thickness(5),
#else
                            Margin = new Thickness(5),
#endif
                            Tag = j < 3 ? "b" : "w",
                            Name = String.Format("img{0}{1}", i, j)

                        };
                        // var field = (Grid)FindName(String.Format("f{0}{1}", i, j));
                        cellBoard.Children.Add(checker);
                    }
                }
            }
        }

        private void f_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Grid currentSender = (Grid)sender;
            //if (currentSender.Children.Count != 0 && SelectedChecker == null)
            if (currentSender.Children.Count != 0)
            {
                SelectedChecker = currentSender;
            }
            else if (currentSender.Children.Count == 0 && SelectedChecker != null)
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
                            MoveChecker(currentGridChecker, newGridChecker);
                            ResetSteps();
                        }
                    }
                    else
                    {
                        var isKill = OneFieldKill(currentGridChecker, newGridChecker);
                        #region для обязательного битья неограниченного кол-ва шашек подряд

                        DeleteAndMove();

                        bool isReset = true;
                        var listCombination = NeedKillForCheckers(CurrentCheckers);
                        if (CurrentCheckers == CheckerColors.White && LastKillCheckerWhite != null)
                        {
                            foreach (var list in listCombination)
                            {
                                var grid = (Grid)FindName(String.Format("f{0}{1}", list[0].Column, list[0].Row));
                                if (grid == LastKillCheckerWhite)
                                {
                                    isReset = false;
                                    break;
                                }
                            }
                            //если при битье шашка переходит в дамку, то проверяем нужно ли этой дамке сразу бить
                            if (isReset)
                            {
                                var listDCombination = NeedKillForDCheckers(CurrentCheckers);
                                foreach (var list in listDCombination)
                                {
                                    var grid = (Grid)FindName(String.Format("f{0}{1}", list[0].Column, list[0].Row));
                                    if (grid == LastKillCheckerWhite)
                                    {
                                        isReset = false;
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
                                    isReset = false;
                                    break;
                                }
                            }
                            //если при битье шашка переходит в дамку, то проверяем нужно ли этой дамке сразу бить
                            if (isReset)
                            {
                                var listDCombination = NeedKillForDCheckers(CurrentCheckers);
                                foreach (var list in listDCombination)
                                {
                                    var grid = (Grid)FindName(String.Format("f{0}{1}", list[0].Column, list[0].Row));
                                    if (grid == LastKillCheckerBlack)
                                    {
                                        isReset = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (isReset && isKill)
                        {
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
            if (CurrentCheckers == CheckerColors.White)
            {
                CurrentCheckers = CheckerColors.Black;
            }
            else
            {
                CurrentCheckers = CheckerColors.White;
            }
        }

        //private void MoveChecker(Grid currentGrid, Grid newGrid)
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
        //}
        //private Image TempImage { get; set; }
        private void MoveChecker(GridChecker currentGridChecker, GridChecker newGridChecker)
        {
            Sync = SynchronizationContext.Current;
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
            Canvas.SetZIndex(currentGrid, MaxZIndex++);
            // Create a red rectangle that will be the target
            // of the animation.

            var transform1 = currentGrid.TransformToVisual(gameGrid);
            var coordinateCurrentGrid = transform1.TransformPoint(new Point(0, 0));

            var transform2 = newGrid.TransformToVisual(gameGrid);
            var coordinateNewGrid = transform2.TransformPoint(new Point(0, 0));

            var offsetX = coordinateNewGrid.X - coordinateCurrentGrid.X;
            var offsetY = coordinateNewGrid.Y - coordinateCurrentGrid.Y;


            // Create the transform
            TranslateTransform moveTransform = new TranslateTransform();
            moveTransform.X = 0;
            moveTransform.Y = 0;

            newImg.RenderTransform = moveTransform;

            // Create a duration of 2 seconds.
            Duration duration = new Duration(TimeSpan.FromSeconds(0.5));

            // Create two DoubleAnimations and set their properties.
            DoubleAnimation myDoubleAnimationX = new DoubleAnimation();
            DoubleAnimation myDoubleAnimationY = new DoubleAnimation();

            myDoubleAnimationX.Duration = duration;
            myDoubleAnimationY.Duration = duration;

            Storyboard justintimeStoryboard = new Storyboard();
            justintimeStoryboard.Duration = duration;

            justintimeStoryboard.Children.Add(myDoubleAnimationX);
            justintimeStoryboard.Children.Add(myDoubleAnimationY);

            Storyboard.SetTarget(myDoubleAnimationX, moveTransform);
            Storyboard.SetTarget(myDoubleAnimationY, moveTransform);

            // Set the X and Y properties of the Transform to be the target properties
            // of the two respective DoubleAnimations.
            Storyboard.SetTargetProperty(myDoubleAnimationX, "X");
            Storyboard.SetTargetProperty(myDoubleAnimationY, "Y");

            myDoubleAnimationX.To = offsetX;
            myDoubleAnimationY.To = offsetY;

            // Make the Storyboard a resource.
            this.Resources.Remove("justintimeStoryboard");
            this.Resources.Add("justintimeStoryboard", justintimeStoryboard);

            // Begin the animation.
            justintimeStoryboard.Begin();
            justintimeStoryboard.Completed += delegate
            {
                DeleteAndMove();
            };

            //new System.Threading.ManualResetEvent(false).WaitOne(500);
            //IsDelayCompleted = false;
            //Timer.Interval = TimeSpan.FromMilliseconds(500);
            //Timer.Tick += new EventHandler(DeleteAndMove);
            //{
            //    Timer.Stop();

            //};
            //Timer.Start();
            TempCurrentGrid = currentGrid;
            TempNewGrid = newGrid;
            TempNewImg = newImg;
            TempStoryboard = justintimeStoryboard;
            //await Task.Delay(TimeSpan.FromMilliseconds(500));
            

        }
        private Grid TempCurrentGrid = null;
        private Grid TempNewGrid = null;
        private Image TempNewImg = null;
        private Storyboard TempStoryboard = null;

        private void DeleteAndMove()
        {
            if (TempCurrentGrid == null) return;
            DeleteChecker(TempCurrentGrid);

            TempNewGrid.Children.Add(new Image
            {
                Tag = TempNewImg.Tag.ToString(),
                Source = TempNewImg.Source,
#if WINDOWS_PHONE_APP
                Margin = new Thickness(5),
#else
                Margin = new Thickness(5),
#endif
                Name = TempNewImg.Name// + Guid.NewGuid()
            });
            TempCurrentGrid = null;
            TempNewGrid = null;
            TempNewImg = null;
            TempStoryboard = null;
        }

        //private bool IsDelayCompleted = true;

        private void DeleteChecker(Grid grid)
        {
            grid.Children.Clear();
        }

        private void DeleteChecker(GridChecker gridChecker)
        {
            Grid grid = (Grid)FindName(String.Format("f{0}{1}", gridChecker.Column, gridChecker.Row));
            grid.Children.Clear();
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
                MoveChecker(currentGridChecker, newGridChecker);
                lastChecker = (Grid)FindName(String.Format("f{0}{1}", newGridChecker.Column, newGridChecker.Row));
            }
            //северо-запад
            else if (newGridChecker.Column - currentGridChecker.Column == -2 && newGridChecker.Row - currentGridChecker.Row == -2 && matrix[currentGridChecker.Column - 1, currentGridChecker.Row - 1].Contains(color))
            {
                var grid = (Grid)FindName(String.Format("f{0}{1}", currentGridChecker.Column - 1, currentGridChecker.Row - 1));
                DeleteChecker(grid);
                MoveChecker(currentGridChecker, newGridChecker);
                lastChecker = (Grid)FindName(String.Format("f{0}{1}", newGridChecker.Column, newGridChecker.Row));
            }
            //юго-запад
            else if (newGridChecker.Column - currentGridChecker.Column == -2 && newGridChecker.Row - currentGridChecker.Row == 2 && matrix[currentGridChecker.Column - 1, currentGridChecker.Row + 1].Contains(color))
            {
                var grid = (Grid)FindName(String.Format("f{0}{1}", currentGridChecker.Column - 1, currentGridChecker.Row + 1));
                DeleteChecker(grid);
                MoveChecker(currentGridChecker, newGridChecker);
                lastChecker = (Grid)FindName(String.Format("f{0}{1}", newGridChecker.Column, newGridChecker.Row));
            }
            //юго-восток
            else if (newGridChecker.Column - currentGridChecker.Column == 2 && newGridChecker.Row - currentGridChecker.Row == 2 && matrix[currentGridChecker.Column + 1, currentGridChecker.Row + 1].Contains(color))
            {
                var grid = (Grid)FindName(String.Format("f{0}{1}", currentGridChecker.Column + 1, currentGridChecker.Row + 1));
                DeleteChecker(grid);
                MoveChecker(currentGridChecker, newGridChecker);
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
                    MoveChecker(currentGridChecker, newGridChecker);//то ходим дамкой
                    DeleteAndMove();

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
                    bool isReset = true;
                    var listCombination = NeedKillForDCheckers(CurrentCheckers);
                    if (CurrentCheckers == CheckerColors.White && LastKillDCheckerWhite != null)
                    {
                        foreach (var listComb in listCombination)
                        {
                            var grid = (Grid)FindName(String.Format("f{0}{1}", listComb[0].Column, listComb[0].Row));
                            if (grid == LastKillDCheckerWhite)
                            {
                                isReset = false;
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
                                isReset = false;
                                break;
                            }
                        }
                    }
                    if (isReset)
                    {
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
                    DeleteAndMove();

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
                    bool isReset = true;
                    var listCombination = NeedKillForDCheckers(CurrentCheckers);
                    if (CurrentCheckers == CheckerColors.White && LastKillDCheckerWhite != null)
                    {
                        foreach (var listComb in listCombination)
                        {
                            var grid = (Grid)FindName(String.Format("f{0}{1}", listComb[0].Column, listComb[0].Row));
                            if (grid == LastKillDCheckerWhite)
                            {
                                isReset = false;
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
                                isReset = false;
                                break;
                            }
                        }
                    }
                    if (isReset)
                    {
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
