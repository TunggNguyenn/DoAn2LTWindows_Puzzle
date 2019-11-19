using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.Windows.Threading;


namespace _1712872
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///
    public enum GAME_MOVE
    {
        MOVE_LEFT,
        MOVE_RIGHT,
        MOVE_UP,
        MOVE_DOWN
    }
    public class UI_GameManagerComunicate
    {
        public delegate void ConnectImageSource(Image target, bool isReviewPic);
        public delegate void GameStart(FrameworkElement window);
        public delegate void GameMoving(FrameworkElement window, GAME_MOVE movingCode);
        public delegate void ClickMove(int mouse_X, int mouse_Y, FrameworkElement window);
        public delegate void ConnectClockToUI(string timeString);
        public delegate void ControlUI(bool isActive);
        public delegate void PicFollowTheMouse(Point startMousePos, Point lastMousePos);
        public delegate void SnapImage(Point startMousePos, Point lastMousePos, FrameworkElement window);
         
        public static ConnectImageSource linkImageToGameManager;
        public static GameStart start;
        public static GameMoving controlMove;
        public static ClickMove Click;
        public static ConnectClockToUI onEverySecond;
        public static ControlUI clockRemote;
        public static ControlUI lockScreen;
        public static PicFollowTheMouse dragPic;
        public static SnapImage fixPicPosition;
    }
        

    public class GameModel
    {
        public int[,] model = new int[3, 3];
        public void setupModel(int rows, int cols)
        {
            for (int i = 0; i < rows; i++)           
                for (int j = 0; j < cols; j++)
                    model[i, j] = (cols * i + j);
             
        }
    }
    
    public class BlackBox // t khong biet dat ten la gi nua :)
    {
        public int location;
        public int picLeft;
        public int picRight;
        public int picUp;
        public int picDown;
        public int currentRow;
        public int currentCol;

        public void setupCube()
        {
            this.location = 8;
            this.picLeft = 7;
            this.picUp = 5;

            this.picRight = -1;
            this.picDown = -1;

            this.currentCol = 2;
            this.currentRow = 2;
        }
    }

    public class GameManager
    {
        //------------------------ VARIABLE AREA --------------------------
        private int _cols = 3;
        private int _rows = 3;
        private int _previewImgHeight = 100;
        private int _previewImgWidth = 150;
        private int _imgHeight = 100;
        private int _imgWidth = 150;
        private int _defautTopSpace = 50;
        private int _defaultLeftSpace = 10;
        private int _clock;
        private int _timeDelay = 60 * 10; // 10 minutes for 1 game

        private DispatcherTimer _timeCounter;
        private double _animationSpeed = 0.5;
        private BitmapImage _imgSource;


        public BlackBox emptyPiece = new BlackBox();
        public GameModel gameModel = new GameModel();
        public List<Image> imgList = new List<Image>();
        public Image previewImg;

       
        //-------------------------- END VARIABLE AREA ---------------- 

        //-------------------------- SETUP GAME AREA ------------------
        private bool setupListImg()
        {
            if (pickApicture())
            {

                int rectHeight = (int)_imgSource.Height / (_rows);
                int rectWidth = (int)_imgSource.Width / (_cols);
                fixImgListSize();
                for (int i = 0; i < _rows; i++)
                    for (int j = 0; j < _cols; j++)
                    {
                        if (i == _rows - 1 && j == _cols - 1)
                            continue;

                        imgList[i * _cols + j].Source = cutImage(j * rectWidth, i * rectHeight, rectWidth, rectHeight);
                        imgList[i * _cols + j].Stretch = Stretch.Fill; // very important, without it image dont fix with width and height
                    }

                
                return true;
            }

            return false;
        }

        private void setupClock()
        {
            _clock = _timeDelay;

            if(_timeCounter == null)
            {
                _timeCounter = new DispatcherTimer();
                _timeCounter.Interval = TimeSpan.FromSeconds(1);
                _timeCounter.Tick += _timeCounter_Tick;
            }

            _timeCounter.Start();
        }

        private void createGame(FrameworkElement window)
        {

            if (!setupListImg())
            {
                MessageBox.Show("Game setup fail because you don't choose a pic :(( , i hate you, we broke up");
                return;
            }
            emptyPiece.setupCube();
            gameModel.setupModel(_cols, _rows);
            setupClock();
            _timeCounter.Stop();
            previewImg.Height = _previewImgHeight;
            previewImg.Width = _previewImgWidth;
            shuffle(window);
            _timeCounter.Start();
        }


        //-------------------------- END SETUP GAME AREA ------------------

        //-------------------------- HANDLE INSIDLE CLASS ------------------

        // hanlde setup img
        private bool pickApicture()
        {
            var screen = new OpenFileDialog();
            if(screen.ShowDialog() == true)
            {
                _imgSource = new BitmapImage(new Uri(screen.FileName, UriKind.RelativeOrAbsolute));

                previewImg.Source = _imgSource;
                previewImg.Height = _previewImgHeight;
                previewImg.Width = _previewImgWidth;

                return true;
            }

            return false;
        }

        private CroppedBitmap cutImage(int X_StartPos, int Y_StartPos, int rectWidth, int rectHeight)
        {
            var rect = new Int32Rect(X_StartPos, Y_StartPos, rectWidth, rectHeight);
            return new CroppedBitmap(_imgSource, rect);
        }

        private void fixImgListSize()
        {
            for(int i = 0; i < imgList.Count; i++)
            {
                imgList[i].Height = _imgHeight;
                imgList[i].Width = _imgWidth;

                Canvas.SetTop(imgList[i], _defautTopSpace + ((int)(i / 3) * _imgHeight));
                Canvas.SetLeft(imgList[i], _defaultLeftSpace + ((int)(i % 3) * _imgWidth));
            }
        }

        //time
        private void _timeCounter_Tick(object sender, EventArgs e)
        {
            _clock--;
            StringBuilder timeDis = new StringBuilder();
            timeDis.Append(_clock / 60);
            timeDis.Append(" : ");
            timeDis.Append(_clock % 60);
            UI_GameManagerComunicate.onEverySecond(timeDis.ToString());
        }

        //moving
        private void UIAnimation(int picID, int from, int to, bool isHorizontal, FrameworkElement window)
        {
            var animation = new DoubleAnimation();

            animation.FillBehavior = FillBehavior.Stop;

            animation.From = from;
            animation.To = to;

            animation.Duration = new Duration(TimeSpan.FromSeconds(_animationSpeed));

            var story = new Storyboard();
            story.Children.Add(animation);
            Storyboard.SetTargetName(animation, imgList[picID].Name);

            if (isHorizontal)
                Storyboard.SetTargetProperty(animation, new PropertyPath(Canvas.LeftProperty));
            else
                Storyboard.SetTargetProperty(animation, new PropertyPath (Canvas.TopProperty));

            story.Begin(window);
            if (isHorizontal)
                Canvas.SetLeft(imgList[picID], to);
            else
                Canvas.SetTop(imgList[picID], to);
        }
        private void swapModelValue(Tuple<int,int> firstIndex, Tuple <int,int> secondIndex)
        {
            int temp = gameModel.model[firstIndex.Item1, firstIndex.Item2];

            gameModel.model[firstIndex.Item1, firstIndex.Item2] = gameModel.model[secondIndex.Item1, secondIndex.Item2];

            gameModel.model[secondIndex.Item1, secondIndex.Item2] = temp;
        }

        private void moveLeft(FrameworkElement window, bool isNeedUpdateUI = true)
        {
            if (emptyPiece.picLeft == -1)
                return;

            //model update
            emptyPiece.picRight = emptyPiece.picLeft;

            emptyPiece.location -= 1;
            emptyPiece.currentCol -= 1;

            if (emptyPiece.location % 3 == 0) // nam o bien trai
                emptyPiece.picLeft = -1;
            else
                emptyPiece.picLeft = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol - 1];

            if (emptyPiece.location < 3) // nam o bien tren
                emptyPiece.picUp = -1;
            else
                emptyPiece.picUp = gameModel.model[emptyPiece.currentRow - 1, emptyPiece.currentCol];

            if (emptyPiece.location > 5) // nam o bien duoi
                emptyPiece.picDown = -1;
            else
                emptyPiece.picDown = gameModel.model[emptyPiece.currentRow + 1, emptyPiece.currentCol];

            swapModelValue(new Tuple<int, int>(emptyPiece.currentRow, emptyPiece.currentCol), new Tuple<int, int>(emptyPiece.currentRow, emptyPiece.currentCol + 1));

            //update UI
            if(isNeedUpdateUI)
                UIAnimation(emptyPiece.picRight, _defaultLeftSpace + emptyPiece.currentCol * _imgWidth, _defaultLeftSpace + (emptyPiece.currentCol + 1) * _imgWidth, true,window);

        }

        private void moveRight(FrameworkElement window, bool isNeedUpdateUI = true)
        {
            if (emptyPiece.picRight == -1)
                return;
            //model update
            emptyPiece.picLeft = emptyPiece.picRight;
            emptyPiece.location += 1;
            emptyPiece.currentCol += 1;

            if (emptyPiece.location % 3 == 2) // nam o bien phai
                emptyPiece.picRight = -1;
            else
                emptyPiece.picRight = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol + 1];

            if (emptyPiece.location < 3) // nam o bien tren
                emptyPiece.picUp = -1;
            else
                emptyPiece.picUp = gameModel.model[emptyPiece.currentRow - 1, emptyPiece.currentCol];

            if (emptyPiece.location > 5) // nam o bien duoi
                emptyPiece.picDown = -1;
            else
                emptyPiece.picDown = gameModel.model[emptyPiece.currentRow + 1, emptyPiece.currentCol];

            swapModelValue(new Tuple<int, int>(emptyPiece.currentRow, emptyPiece.currentCol), new Tuple<int, int>(emptyPiece.currentRow, emptyPiece.currentCol - 1));

            //update UI
            if (isNeedUpdateUI)
                UIAnimation(emptyPiece.picLeft, _defaultLeftSpace + emptyPiece.currentCol * _imgWidth, _defaultLeftSpace + (emptyPiece.currentCol - 1) * _imgWidth, true, window);
        }

        private void moveUp(FrameworkElement window, bool isNeedUpdateUI = true)
        {
            if (emptyPiece.picUp == -1)
                return;           

            //model update
            emptyPiece.picDown = emptyPiece.picUp;
            emptyPiece.location -= 3;
            emptyPiece.currentRow -= 1;

            if (emptyPiece.location < 3) // nam o bien tren
                emptyPiece.picUp = -1;
            else
                emptyPiece.picUp = gameModel.model[emptyPiece.currentRow - 1, emptyPiece.currentCol];

            if (emptyPiece.location % 3 == 0) // nam o bien trai
                emptyPiece.picLeft = -1;
            else
                emptyPiece.picLeft = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol - 1];

            if (emptyPiece.location % 3 == 2) // nam o bien phai
                emptyPiece.picRight = -1;
            else
                emptyPiece.picRight = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol + 1];

            swapModelValue(new Tuple<int, int>(emptyPiece.currentRow, emptyPiece.currentCol), new Tuple<int, int>(emptyPiece.currentRow + 1, emptyPiece.currentCol));

            //update UI
            if (isNeedUpdateUI)
                UIAnimation( emptyPiece.picDown, _defautTopSpace + emptyPiece.currentRow * _imgHeight, _defautTopSpace + (emptyPiece.currentRow + 1) * _imgHeight, false, window);
        }

        private void moveDown(FrameworkElement window, bool isNeedUpdateUI = true)
        {
            if (emptyPiece.picDown == -1)
                return;

            //model update
            emptyPiece.picUp = emptyPiece.picDown;
            emptyPiece.location += 3;
            emptyPiece.currentRow += 1;

            if (emptyPiece.location > 5) // nam o bien duoi
                emptyPiece.picDown = -1;
            else
                emptyPiece.picDown = gameModel.model[emptyPiece.currentRow + 1, emptyPiece.currentCol];

            if (emptyPiece.location % 3 == 0) // nam o bien trai
                emptyPiece.picLeft = -1;
            else
                emptyPiece.picLeft = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol - 1];

            if (emptyPiece.location % 3 == 2) // nam o bien phai
                emptyPiece.picRight = -1;
            else
                emptyPiece.picRight = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol + 1];

            swapModelValue(new Tuple<int, int>(emptyPiece.currentRow, emptyPiece.currentCol), new Tuple<int, int>(emptyPiece.currentRow - 1, emptyPiece.currentCol));

            //update UI
            if (isNeedUpdateUI)
                UIAnimation(emptyPiece.picUp, _defautTopSpace + emptyPiece.currentRow * _imgHeight, _defautTopSpace + (emptyPiece.currentRow - 1) * _imgHeight, false, window);
        }
        // end moving

        // support for start game
        private void shuffle(FrameworkElement window)
        {
            var rng = new Random();
            int next;
            for(int i = 0; i < 120; ++i)
            {
                next = rng.Next(4);
                switch(next)
                {
                    case 0:
                        moveLeft(window);
                        break;
                    case 1:
                        moveRight(window);
                        break;
                    case 2:
                        moveUp(window);
                        break;
                    case 3:
                        moveDown(window);
                        break;
                    default:
                        break;
                }
            }
        }
        //win condition
        private bool isPuzzleComplete()
        {
            
            for (int i = 0; i < _rows; ++i)
                for (int j = 0; j < _cols; ++j)
                    if (gameModel.model[i, j] != i * _rows + j)
                        return false;
            return true;
        }

        private void checkGameStatus()
        {
            if (_clock <= 0)
                MessageBox.Show("You are loser");
            else if (isPuzzleComplete())
            {
                UI_GameManagerComunicate.lockScreen(true);
                MessageBox.Show("You win");
            }
            
        }
        private int getColFromCoordinateMouse(int mouse_X)
        {
            return (mouse_X - _defaultLeftSpace) / _imgWidth;
        }
        private int getRowFromCoordinateMouse(int mouse_Y)
        {
            return (mouse_Y - _defautTopSpace) / _imgHeight;
        }
        private int getPicIDFromCoordinateMouse(int mouse_X, int mouse_Y)
        {
            int colIndex = getColFromCoordinateMouse(mouse_X);
            int rowIndex = getRowFromCoordinateMouse(mouse_Y);

            if (colIndex < 0 || colIndex > _cols || rowIndex < 0 || rowIndex > _rows)
                return -1;
            return gameModel.model[rowIndex, colIndex];
        }

        //------------------------------ END HANDLE INSIDLE CLASS ------------------

        //------------------------------ DELEGATE FUNCTION -------------------------
        private void addImg(Image target, bool isReviewPic)
        {
            if (isReviewPic)
                previewImg = target;
            else
                imgList.Add(target);
        }

        private void handleMove(FrameworkElement window, GAME_MOVE moveCode)
        {
            switch (moveCode)
            {
                case GAME_MOVE.MOVE_LEFT:
                    moveLeft(window);
                    break;
                case GAME_MOVE.MOVE_RIGHT:
                    moveRight(window);
                    break;
                case GAME_MOVE.MOVE_UP:
                    moveUp(window);
                    break;
                case GAME_MOVE.MOVE_DOWN:
                    moveDown(window);
                    break;
                default:
                    break;
            }

            checkGameStatus();
        }
       
        private void onClick(int mouse_X, int mouse_Y, FrameworkElement window)
        {

            int picID = getPicIDFromCoordinateMouse(mouse_X, mouse_Y);

            if (picID == emptyPiece.picLeft)
                moveLeft(window);
            else if (picID == emptyPiece.picRight)
                moveRight(window);
            else if (picID == emptyPiece.picUp)
                moveUp(window);
            else if (picID == emptyPiece.picDown)
                moveDown(window);
        }

        private void changeClockStatus(bool isRun)
        {
            if (isRun)
                _timeCounter.Start();
            else
                _timeCounter.Stop();
        }

        private void dragPicture(Point startMousePos, Point lastMousePos)
        {
            int picID = getPicIDFromCoordinateMouse((int)startMousePos.X, (int)startMousePos.Y);

            Canvas.SetLeft(imgList[picID], lastMousePos.X);
            Canvas.SetTop(imgList[picID], lastMousePos.Y);
        }

        private void snapPicture(Point startMousePos, Point lastMousePos, FrameworkElement window)
        {
            bool canMove = false;
            int originCol = getColFromCoordinateMouse((int)startMousePos.X);
            int originRow = getRowFromCoordinateMouse((int)startMousePos.Y);

            int picID =  gameModel.model[originRow, originCol];

            int colIndex = getColFromCoordinateMouse((int)lastMousePos.X);
            int rowIndex = getRowFromCoordinateMouse((int)lastMousePos.Y);
            if (colIndex == emptyPiece.currentCol && rowIndex == emptyPiece.currentRow)
            {
                if (picID == emptyPiece.picLeft)
                {
                    moveLeft(window, false);
                    canMove = true;
                }
                else if (picID == emptyPiece.picRight)
                {
                    moveRight(window, false);
                    canMove = true;
                }
                else if (picID == emptyPiece.picUp)
                {
                    moveUp(window, false);
                    canMove = true;
                }
                else if (picID == emptyPiece.picDown)
                {
                    moveDown(window, false);
                    canMove = true;
                }
            }

            if(canMove)
            {
                Canvas.SetLeft(imgList[picID], _defaultLeftSpace + _imgWidth * colIndex);
                Canvas.SetTop(imgList[picID], _defautTopSpace + _imgHeight * rowIndex);
            }
            else
            {
                Canvas.SetLeft(imgList[picID], _defaultLeftSpace + _imgWidth * originCol);
                Canvas.SetTop(imgList[picID], _defautTopSpace + _imgHeight * originRow);
            }
        }
        //------------------------------ END DELEGATE FUNCTION ----------------------


        //------------------------------- PUBLIC AREA -------------------
        
        public void setupDelegate()
        {
            UI_GameManagerComunicate.linkImageToGameManager = addImg;
            UI_GameManagerComunicate.start = createGame;
            UI_GameManagerComunicate.controlMove = handleMove;
            UI_GameManagerComunicate.Click = onClick;
            UI_GameManagerComunicate.clockRemote = changeClockStatus;
            UI_GameManagerComunicate.dragPic = dragPicture;
            UI_GameManagerComunicate.fixPicPosition = snapPicture;
        }
        //--------------------------- END PUBLIC AREA ---------------------------

    }
    public partial class MainWindow : Window
    {
        bool isGameRun = false;
        bool isDragging = false;
        Point lastMousePos = new Point();
        Point startMousePos = new Point();
        public MainWindow()
        {
            InitializeComponent();
            GameManager gameManager = new GameManager();
            gameManager.setupDelegate();
            UI_GameManagerComunicate.onEverySecond = ticTac;
            UI_GameManagerComunicate.lockScreen = screenControl;
            activeLinkImg();
            

        }

        private void activeLinkImg()
        {
            UI_GameManagerComunicate.linkImageToGameManager(img1, false);
            UI_GameManagerComunicate.linkImageToGameManager(img2, false);
            UI_GameManagerComunicate.linkImageToGameManager(img3, false);
            UI_GameManagerComunicate.linkImageToGameManager(img4, false);
            UI_GameManagerComunicate.linkImageToGameManager(img5, false);
            UI_GameManagerComunicate.linkImageToGameManager(img6, false);
            UI_GameManagerComunicate.linkImageToGameManager(img7, false);
            UI_GameManagerComunicate.linkImageToGameManager(img8, false);

            UI_GameManagerComunicate.linkImageToGameManager(picTemplate, true);
        }
        private void ticTac(string timeString)
        {
            timeDisplay.Text = timeString;
        }
        private void screenControl(bool isActive)
        {
            if (isActive)
                isGameRun = false;
            else
                isGameRun = true;
        }
        private void startGameBtn_Click(object sender, RoutedEventArgs e)
        {
            isGameRun = true;
            UI_GameManagerComunicate.start(this);
        }

        private void Exitbtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (isGameRun)
            {
                if (e.Key == Key.D || e.Key == Key.Right)
                    UI_GameManagerComunicate.controlMove(this, GAME_MOVE.MOVE_LEFT);
                if (e.Key == Key.A || e.Key == Key.Left)
                    UI_GameManagerComunicate.controlMove(this, GAME_MOVE.MOVE_RIGHT);
                if (e.Key == Key.W || e.Key == Key.Up)
                    UI_GameManagerComunicate.controlMove(this, GAME_MOVE.MOVE_DOWN);
                if (e.Key == Key.S || e.Key == Key.Down)
                    UI_GameManagerComunicate.controlMove(this, GAME_MOVE.MOVE_UP);
            }

        }

        private void gameScreen_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isGameRun)
            {
                Point currentMousePos = Mouse.GetPosition(gameScreen);
                isDragging = false;
                if (-2 <= startMousePos.X - currentMousePos.X && startMousePos.X - currentMousePos.X <= 2 && -2 <= startMousePos.Y - currentMousePos.Y && startMousePos.Y - currentMousePos.Y <= 2)
                    UI_GameManagerComunicate.Click((int)Mouse.GetPosition(gameScreen).X, (int)Mouse.GetPosition(gameScreen).Y, this);
                else // snap pic
                    UI_GameManagerComunicate.fixPicPosition(startMousePos, lastMousePos, this);
            }
        }

        private void pauseBtn_Click(object sender, RoutedEventArgs e)
        {
            continueBtn.IsEnabled = true;
            pauseBtn.IsEnabled = false;
            UI_GameManagerComunicate.clockRemote(false);
            UI_GameManagerComunicate.lockScreen(true);
        }

        private void continueBtn_Click(object sender, RoutedEventArgs e)
        {
            continueBtn.IsEnabled = false;
            pauseBtn.IsEnabled = true;
            UI_GameManagerComunicate.clockRemote(true);
            UI_GameManagerComunicate.lockScreen(false);
        }

        private void gameScreen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isGameRun)
            {
                isDragging = true;
                startMousePos = Mouse.GetPosition(gameScreen);
            }
           
        }

        private void gameScreen_MouseMove(object sender, MouseEventArgs e)
        {
            if(isDragging && isGameRun)
            {
                lastMousePos = Mouse.GetPosition(gameScreen);
                UI_GameManagerComunicate.dragPic(startMousePos, lastMousePos);
               
            }
        }
    }
}
