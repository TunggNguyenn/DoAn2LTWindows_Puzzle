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
using System.IO;
using System.Diagnostics;

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
        public delegate void GameStart(FrameworkElement window,bool isLoadedGame=false);
        public delegate void GameMoving(FrameworkElement window, GAME_MOVE movingCode);
        public delegate void GameSave();
        public delegate void GameLoad();

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
        public static GameSave save;
        public static GameLoad load;
    }


    public class GameModel
    {
        public int[,] model = null;
        public void setupModel(int rows, int cols)
        {
            model = new int[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    model[i, j] = (cols * i + j);
                }
            }

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
        public int rows;
        public int cols;
       
        public BlackBox(int rows,int cols)
        {
            this.rows = rows;
            this.cols = cols;
        }

        public void setupCube()
        {
            this.location = rows * cols - 1;
            this.picLeft = rows * cols - 2;
            this.picUp = rows * cols - rows - 1; ;

            this.picRight = -1;
            this.picDown = -1;

            this.currentCol = cols - 1; 
            this.currentRow = rows - 1;
        }
    }



    public class GameManager
    {
        //------------------------ VARIABLE AREA --------------------------
        public  int _cols;
        public  int _rows;
        public  int _previewImgHeight = 200;
        public  int _previewImgWidth = 200;
        private int _imgHeight = 60;
        private int _imgWidth = 60;
        private int _defautTopSpace = 30;
        private int _defaultLeftSpace = 30;
        private  int _clock;
        private  int _timeDelay = 60 * 10; // 10 minutes for 1 gamestatic 

        private DispatcherTimer _timeCounter=null;
        private double _animationSpeed = 0.5;
        private BitmapImage  _imgSource;


        public BlackBox emptyPiece = null;
        public GameModel gameModel = new GameModel();
        private List<Image> imgList = new List<Image>();
        private Image previewImg = new Image();
        private  string _imageURL;
        private  string recentTime;
        //-------------------------- END VARIABLE AREA ---------------- 

        //-------------------------- SETUP GAME AREA ------------------
        public void changeModel(int rows,int cols)
        {
            this._rows = rows;
            this._cols = cols;
        }

        public GameManager(int rows, int cols)
        {
            this._rows = rows;
            this._cols = cols;
            emptyPiece = new BlackBox(_rows, _cols);
    }
        public void setUpModelImg()
        {
            int picID;
            for (int i = 0; i < _rows; i++)
                for (int j = 0; j < _cols; j++)
                {
                    if (gameModel.model[i, j] == _rows * _cols - 1)
                    {
                        continue;
                    }
                    picID = gameModel.model[i, j];
                    Canvas.SetLeft(imgList[picID], _defaultLeftSpace + j * _imgWidth);
                    Canvas.SetTop(imgList[picID], _defautTopSpace + i * _imgHeight);                  
                }
        }

        public bool setupListImg(bool isLoadedGame=false)
        {
            if (pickApicture(isLoadedGame))
            {
                int rectHeight = (int)_imgSource.Height / (_rows);
                int rectWidth = (int)_imgSource.Width / (_cols);
                fixImgListSize();

                for (int i = 0; i < _rows; i++)
                    for (int j = 0; j < _cols; j++)
                    {
                        if (i == _rows - 1 && j == _cols - 1)
                        {                           
                            continue;
                        }
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

            if (_timeCounter == null)
            {
                _timeCounter = new DispatcherTimer();
                _timeCounter.Interval = TimeSpan.FromSeconds(1);
                _timeCounter.Tick += _timeCounter_Tick;
            }

            _timeCounter.Start();
        }
        public void setNewEmptyPiece(ref BlackBox emptyPiece)
        {
            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _cols; j++)
                {
                    if (gameModel.model[i, j] == _rows * _cols - 1)
                    {
                        emptyPiece.location = i * _cols + j;
                        emptyPiece.currentCol = j;
                        emptyPiece.currentRow = i;
                        if (emptyPiece.location % _rows == 0) // nam o bien trai
                            emptyPiece.picLeft = -1;
                        else
                            emptyPiece.picLeft = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol - 1];

                        if (emptyPiece.location < _rows) // nam o bien tren
                            emptyPiece.picUp = -1;
                        else
                            emptyPiece.picUp = gameModel.model[emptyPiece.currentRow - 1, emptyPiece.currentCol];

                        if (emptyPiece.location > _rows * _cols - _rows - 1) // nam o bien duoi
                            emptyPiece.picDown = -1;
                        else
                            emptyPiece.picDown = gameModel.model[emptyPiece.currentRow + 1, emptyPiece.currentCol];
                        if (emptyPiece.location % _rows == 2) // nam o ben phai
                            emptyPiece.picRight = -1;
                        else
                            emptyPiece.picRight = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol + 1];


                        Debug.WriteLine(emptyPiece.picLeft);
                        Debug.WriteLine(emptyPiece.picUp);
                        Debug.WriteLine(emptyPiece.picRight);
                        Debug.WriteLine(emptyPiece.picDown);

                    }
                }
            }
        }
        private void createGame(FrameworkElement window,bool isLoadedGame=false)
        {
            if (isLoadedGame == false)
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
            else
            {
                setupListImg(isLoadedGame);
                setUpModelImg();
                setupClock();
                setNewEmptyPiece(ref emptyPiece);
            }
        }


        //-------------------------- END SETUP GAME AREA ------------------

        //-------------------------- HANDLE INSIDLE CLASS ------------------


        // hanlde setup img
        private bool pickApicture(bool isLoadGame)
        {
            if (isLoadGame)
            {
                return true;
            }
            else
            {
                var screen = new OpenFileDialog();
                if (screen.ShowDialog() == true)
                {
                    _imageURL = screen.FileName;
                    _imgSource = new BitmapImage(new Uri(_imageURL, UriKind.RelativeOrAbsolute));
                    previewImg.Source = _imgSource;
                    previewImg.Stretch = Stretch.Fill;
                    previewImg.Height = _previewImgHeight;
                    previewImg.Width = _previewImgWidth;

                    return true;
                }
            }
            return false;

        }

        public CroppedBitmap cutImage(int X_StartPos, int Y_StartPos, int rectWidth, int rectHeight)
        {
            var rect = new Int32Rect(X_StartPos, Y_StartPos, rectWidth, rectHeight);
            return new CroppedBitmap(_imgSource, rect);
        }

        private void fixImgListSize()
        {
            for (int i = 0; i < imgList.Count; i++)
            {
                imgList[i].Height = _imgHeight;
                imgList[i].Width = _imgWidth;

                Canvas.SetTop(imgList[i], _defautTopSpace + ((int)(i / _rows) * _imgHeight));
                Canvas.SetLeft(imgList[i], _defaultLeftSpace + ((int)(i % _cols) * _imgWidth));
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

            checkGameStatus();

            recentTime = timeDis.ToString();
            UI_GameManagerComunicate.onEverySecond(recentTime);
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
                Storyboard.SetTargetProperty(animation, new PropertyPath(Canvas.TopProperty));

            story.Begin(window);
            if (isHorizontal)
                Canvas.SetLeft(imgList[picID], to);
            else
                Canvas.SetTop(imgList[picID], to);
        }
        private void swapModelValue(Tuple<int, int> firstIndex, Tuple<int, int> secondIndex)
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

            if (emptyPiece.location % _rows == 0) // nam o bien trai
                emptyPiece.picLeft = -1;
            else
                emptyPiece.picLeft = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol - 1];

            if (emptyPiece.location < _rows) // nam o bien tren
                emptyPiece.picUp = -1;
            else
                emptyPiece.picUp = gameModel.model[emptyPiece.currentRow - 1, emptyPiece.currentCol];

            if (emptyPiece.location > _rows * _cols - _rows - 1) // nam o bien duoi
                emptyPiece.picDown = -1;
            else
                emptyPiece.picDown = gameModel.model[emptyPiece.currentRow + 1, emptyPiece.currentCol];

            swapModelValue(new Tuple<int, int>(emptyPiece.currentRow, emptyPiece.currentCol), new Tuple<int, int>(emptyPiece.currentRow, emptyPiece.currentCol + 1));

            //update UI
            if (isNeedUpdateUI)
                UIAnimation(emptyPiece.picRight, _defaultLeftSpace + emptyPiece.currentCol * _imgWidth, _defaultLeftSpace + (emptyPiece.currentCol + 1) * _imgWidth, true, window);

         
        }

        private void moveRight(FrameworkElement window, bool isNeedUpdateUI = true)
        {
            if (emptyPiece.picRight == -1)
                return;
            //model update
            emptyPiece.picLeft = emptyPiece.picRight;

            emptyPiece.location += 1;
            emptyPiece.currentCol += 1;

            if (emptyPiece.location % _rows == emptyPiece.location) // nam o bien phai
                emptyPiece.picRight = -1;
            else
                emptyPiece.picRight = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol + 1];

            if (emptyPiece.location < _rows) // nam o bien tren
                emptyPiece.picUp = -1;
            else
                emptyPiece.picUp = gameModel.model[emptyPiece.currentRow - 1, emptyPiece.currentCol];

            if (emptyPiece.location > _rows * _cols - _rows - 1) // nam o bien duoi
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
            emptyPiece.location -= _rows;
            emptyPiece.currentRow -= 1;

            if (emptyPiece.location < _rows) // nam o bien tren
                emptyPiece.picUp = -1;
            else
                emptyPiece.picUp = gameModel.model[emptyPiece.currentRow - 1, emptyPiece.currentCol];

            if (emptyPiece.location % _rows == 0) // nam o bien trai
                emptyPiece.picLeft = -1;
            else
                emptyPiece.picLeft = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol - 1];

            if (emptyPiece.location % _rows == emptyPiece.location) // nam o bien phai
                emptyPiece.picRight = -1;
            else
                emptyPiece.picRight = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol + 1];

            swapModelValue(new Tuple<int, int>(emptyPiece.currentRow, emptyPiece.currentCol), new Tuple<int, int>(emptyPiece.currentRow + 1, emptyPiece.currentCol));

            //update UI
            if (isNeedUpdateUI)
                UIAnimation(emptyPiece.picDown, _defautTopSpace + emptyPiece.currentRow * _imgHeight, _defautTopSpace + (emptyPiece.currentRow + 1) * _imgHeight, false, window);
           
        }

        private void moveDown(FrameworkElement window, bool isNeedUpdateUI = true)
        {
            if (emptyPiece.picDown == -1)
                return;

            //model update
            emptyPiece.picUp = emptyPiece.picDown;
            emptyPiece.location += _rows;

            emptyPiece.currentRow += 1;

            if (emptyPiece.location > _rows * _cols - _rows - 1) // nam o bien duoi
                emptyPiece.picDown = -1;
            else
                emptyPiece.picDown = gameModel.model[emptyPiece.currentRow + 1, emptyPiece.currentCol];

            if (emptyPiece.location % _rows == 0) // nam o bien trai
                emptyPiece.picLeft = -1;
            else
                emptyPiece.picLeft = gameModel.model[emptyPiece.currentRow, emptyPiece.currentCol - 1];

            if (emptyPiece.location % _rows == emptyPiece.location) // nam o bien phai
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
            for (int i = 0; i < 120; ++i)
            {
                next = rng.Next(4);
                switch (next)
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
            {
                _timeCounter.Stop();
                MessageBox.Show("You are loser");

            }
            else if (isPuzzleComplete())
            {
                _timeCounter.Stop();
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

            if (colIndex < 0 || colIndex >= _cols || rowIndex < 0 || rowIndex >= _rows)
            {
                return -1;
            }
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

            if (picID == -1 || picID == _rows * _cols - 1)
                return;

            Canvas.SetLeft(imgList[picID], lastMousePos.X - 75);
            Canvas.SetTop(imgList[picID], lastMousePos.Y - 75);
        }

        private void snapPicture(Point startMousePos, Point lastMousePos, FrameworkElement window)
        {
            bool canMove = false;
            int originCol = getColFromCoordinateMouse((int)startMousePos.X);
            int originRow = getRowFromCoordinateMouse((int)startMousePos.Y);

            if (originCol < 0 || originCol >= _cols || originRow < 0 || originRow >= _rows)
                return;

            int picID = gameModel.model[originRow, originCol];

            if (picID == -1 || picID == _rows * _cols - 1)
                return;

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

            if (canMove)
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

        private void saveGame()
        {
            _timeCounter.Stop();
            SaveWindow saveWindow = new SaveWindow();
            saveWindow.ShowDialog();
            if (saveWindow.DialogResult == true)
            {
                string fileName = saveWindow.fileName;
                //mo file
                StreamWriter fileOut = new StreamWriter(fileName);
                fileOut.WriteLine(_imageURL);
                fileOut.WriteLine(recentTime);
                for (int i = 0; i < 3; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        fileOut.Write($"{gameModel.model[i, j]} ");
                    }
                    fileOut.WriteLine("");
                }

                fileOut.Close();
                MessageBox.Show("Saved!");
            }
            ////mo file
            //StreamWriter fileOut = new StreamWriter("save.txt");
            ////ghi nguon anh xuong file
            ///
            else
            {
                MessageBox.Show("You did not enter file name!");
            }
            _timeCounter.Start();

        }
        private void loadGame()
        {
            var loadFile = new OpenFileDialog();
            if (loadFile.ShowDialog() == true)
            {
                //lay file name
                string fileIn = loadFile.FileName;
                var lines = File.ReadAllLines(fileIn);

                //lay hinh
                string img = lines[0];
                ////set hinh
                _imgSource = new BitmapImage(new Uri(img, UriKind.RelativeOrAbsolute));
                previewImg.Source = _imgSource;

                //lay thoi gian
                string time = lines[1];
                const string Split = " : ";
                var timeTokens = time.Split(new string[] { Split }, StringSplitOptions.RemoveEmptyEntries);
                //set time
                _timeDelay = Int32.Parse(timeTokens[0]) * 60 + Int32.Parse(timeTokens[1]);



                //lay model
                int i = 2;
                const string Spliter = " ";
                for (int j = 0; j < _rows; j++)
                {
                    var modelTokens = lines[i].Split(new string[] { Spliter }, StringSplitOptions.RemoveEmptyEntries);
                    for (int k = 0; k < _cols; k++)
                    {
                        gameModel.model[j, k] = Int32.Parse(modelTokens[k]);
                    }
                    i++;
                } 
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
            UI_GameManagerComunicate.save = saveGame;
            UI_GameManagerComunicate.load = loadGame;

        }
        //--------------------------- END PUBLIC AREA ---------------------------
    }
    public partial class MainWindow : Window
    {
        bool isGameRun = false;
        bool isDragging = false;
        Point lastMousePos = new Point();
        Point startMousePos = new Point();

        private int Rows = 5;
        private int Cols = 5;
        private int StartX = 30;
        private int StartY = 30;
        private int Width = 60;
        private int Height = 60;
        GameManager gameManager = null;

        public MainWindow()
        {
            InitializeComponent();
            gameManager = new GameManager(Rows, Cols);
            gameManager.setupDelegate();
            UI_GameManagerComunicate.onEverySecond = ticTac;
            UI_GameManagerComunicate.lockScreen = screenControl;
            //activeLinkImg();
        }

        private void activeLinkImg()
        {
            const string str = "img";

            for (int i = 0; i < Rows * Cols; i++)
            {
                StringBuilder imgName = new StringBuilder();
                imgName.Append(str.ToString());
                imgName.Append(i + 1);

                var img = new Image();
                img.Name = imgName.ToString();

                gameScreen.RegisterName(img.Name, img);
                gameScreen.Children.Add(img);
                UI_GameManagerComunicate.linkImageToGameManager(img, false);

            }

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
            if(!isGameRun)
            {
                return;
            }
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
            if (isDragging && isGameRun)
            {
                lastMousePos = Mouse.GetPosition(gameScreen);
                UI_GameManagerComunicate.dragPic(startMousePos, lastMousePos);

            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            UI_GameManagerComunicate.save();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            UI_GameManagerComunicate.load();
            isGameRun = true;
            UI_GameManagerComunicate.start(this, true);

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DrawingLine();
        }

        private void DrawingLine()
        {
            for (int i = 0; i <= Cols; i++)
            {
                var line = new Line();
                line.X1 = StartX + i * Width;
                line.Y1 = StartY;

                line.X2 = StartX + i * Width;
                line.Y2 = StartY + Cols * Height;

                line.StrokeThickness = 2;

                // Create a red Brush  
                SolidColorBrush redBrush = new SolidColorBrush();
                redBrush.Color = Colors.Red;

                line.Stroke = redBrush;
                gameScreen.Children.Add(line);
            }

            for (int i = 0; i <= Rows; i++)
            {
                var line = new Line();
                line.X1 = StartX;
                line.Y1 = StartY + i * Width;

                line.X2 = StartX + Rows * Width;
                line.Y2 = StartY + i * Width;

                line.StrokeThickness = 2;

                // Create a red Brush  
                SolidColorBrush redBrush = new SolidColorBrush();
                redBrush.Color = Colors.Red;

                line.Stroke = redBrush;

                gameScreen.Children.Add(line);
            }
            activeLinkImg();
            gameManager.changeModel(Rows, Cols);
        }

        private void modeCombx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mode = sender as ComboBox;
            if (mode.SelectedItem.ToString().Contains("3x3"))
            {
                if (gameScreen == null)
                {
                    return;
                }
                clearRegisterName();
                this.Rows = 3;
                this.Cols = 3;
                gameScreen.Children.Clear();
                DrawingLine();
            }
            else if (mode.SelectedItem.ToString().Contains("4x4"))
            {
                if (gameScreen == null)
                {
                    return;
                }
                clearRegisterName();
                this.Rows = 4;
                this.Cols = 4;
                gameScreen.Children.Clear();
                DrawingLine();
            }
            else if (mode.SelectedItem.ToString().Contains("5x5"))
            {
                if (gameScreen == null)
                {
                    return;
                }
                clearRegisterName();
                this.Rows = 5;
                this.Cols = 5;
                gameScreen.Children.Clear();
                DrawingLine();
            }
        }

        private void clearRegisterName()
        {
            const string str = "img";

            for (int i = 0; i < Rows * Cols; i++)
            {
                StringBuilder imgName = new StringBuilder();
                imgName.Append(str.ToString());
                imgName.Append(i + 1);

                gameScreen.UnregisterName(imgName.ToString());
            }
        }
    }
}
