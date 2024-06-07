using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SrcChess2.Core;
using SrcChess2.PgnParsing;

namespace SrcChess2 {   
    public partial class MainWindow : Window {

        #region Types
        private class BoardEvaluationStat(int gameCount) {
            public TimeSpan              Method1Time { get; set; } = TimeSpan.Zero;
            public TimeSpan              Method2Time { get; set; } = TimeSpan.Zero;
            public ChessBoard.GameResult Result { get; set; } = ChessBoard.GameResult.OnGoing;
            public int                   Method1MoveCount { get; set; } = 0;
            public int                   Method2MoveCount { get; set; } = 0;
            public int                   Method1WinCount { get; set; } = 0;
            public int                   Method2WinCount { get; set; } = 0;
            public bool                  UserCancel { get; set; } = false;
            public int                   GameIndex { get; set; } = 0;
            public int                   GameCount { get; set; } = gameCount;
            public  MessageMode          OriMessageMode { get; set; }
        };
        
        //Use for computer move
        public enum MessageMode {
            Silent      = 0,
            CallEndGame = 1,
            Verbose     = 2
        };
        
        /// <summary>Current playing mode</summary>
        public enum MainPlayingMode {
            /// <summary>Player plays against another player</summary>
            PlayerAgainstPlayer,
            /// <summary>Computer play the white against a human black</summary>
            ComputerPlayWhite,
            /// <summary>Computer play the black against a human white</summary>
            ComputerPlayBlack,
            /// <summary>Computer play against computer</summary>
            ComputerPlayBoth,
            /// <summary>Design mode.</summary>
            DesignMode,
            /// <summary>Test evaluation methods. Computer play against itself in loop using two different evaluation methods</summary>
            TestEvaluationMethod
        };
        #endregion

        #region Command
        public static readonly RoutedUICommand      NewGameCommand             = new("_New Game...",                   "NewGame",              typeof(MainWindow));
        public static readonly RoutedUICommand      LoadGameCommand            = new("_Load Game...",                  "LoadGame",             typeof(MainWindow));
        public static readonly RoutedUICommand      LoadPuzzleCommand          = new("Load a Chess P_uzzle...",        "LoadPuzzle",           typeof(MainWindow));
        public static readonly RoutedUICommand      CreateGameCommand          = new("_Create Game from PGN...",       "CreateGame",           typeof(MainWindow));
        public static readonly RoutedUICommand      SaveGameCommand            = new("_Save Game...",                  "SaveGame",             typeof(MainWindow));
        public static readonly RoutedUICommand      QuitCommand                = new("_Quit",                          "Quit",                 typeof(MainWindow));

        public static readonly RoutedUICommand      HintCommand                = new("_Hint",                          "Hint",                 typeof(MainWindow));
        public static readonly RoutedUICommand      UndoCommand                = new("_Undo",                          "Undo",                 typeof(MainWindow));
        public static readonly RoutedUICommand      RedoCommand                = new("_Redo",                          "Redo",                 typeof(MainWindow));
        public static readonly RoutedUICommand      RefreshCommand             = new("Re_fresh",                       "Refresh",              typeof(MainWindow));
        public static readonly RoutedUICommand      SelectPlayersCommand       = new("_Select Players...",             "SelectPlayers",        typeof(MainWindow));
        public static readonly RoutedUICommand      AutomaticPlayCommand       = new("_Automatic Play",                "AutomaticPlay",        typeof(MainWindow));
        public static readonly RoutedUICommand      FastAutomaticPlayCommand   = new("_Fast Automatic Play",           "FastAutomaticPlay",    typeof(MainWindow));
        public static readonly RoutedUICommand      CancelPlayCommand          = new("_Cancel Play",                   "CancelPlay",           typeof(MainWindow));
        public static readonly RoutedUICommand      DesignModeCommand          = new("_Design Mode",                   "DesignMode",           typeof(MainWindow));

        public static readonly RoutedUICommand      ManualSearchSettingCommand = new("_Manual Search Setting...",      "ManualSearchSetting",  typeof(MainWindow));
        public static readonly RoutedUICommand      FlashPieceCommand          = new("_Flash Piece",                   "FlashPiece",           typeof(MainWindow));
        public static readonly RoutedUICommand      ReversedBoardCommand       = new("_Reversed Board",                "ReversedBoard",        typeof(MainWindow));
        public static readonly RoutedUICommand      PgnNotationCommand         = new("_PGN Notation",                  "PGNNotation",          typeof(MainWindow));
        public static readonly RoutedUICommand      BoardSettingCommand        = new("_Board Settings...",             "BoardSettings",         typeof(MainWindow));
        
        public static readonly RoutedUICommand      TestBoardEvaluationCommand = new("_Test Board Evaluation...",      "TestBoardEvaluation",  typeof(MainWindow));

        //List of all supported commands
        private static readonly RoutedUICommand[]   m_arrCommands = [ NewGameCommand,
                                                                      LoadGameCommand,
                                                                      LoadPuzzleCommand,
                                                                      CreateGameCommand,
                                                                      SaveGameCommand,
                                                                      QuitCommand,
                                                                      HintCommand,
                                                                      UndoCommand,
                                                                      RedoCommand,
                                                                      RefreshCommand,
                                                                      SelectPlayersCommand,
                                                                      AutomaticPlayCommand,
                                                                      FastAutomaticPlayCommand,
                                                                      CancelPlayCommand,
                                                                      DesignModeCommand,
                                                                      ManualSearchSettingCommand,
                                                                      FlashPieceCommand,
                                                                      ReversedBoardCommand,
                                                                      PgnNotationCommand,
                                                                      BoardSettingCommand,
                                                                      TestBoardEvaluationCommand];
        #endregion

        #region Members        
        /// <summary>Playing mode (player vs player, player vs computer, computer vs computer</summary>
        private MainPlayingMode                              m_playingMode;
        /// <summary>Color played by the computer</summary>
        public ChessBoard.PlayerColor                        m_computerPlayingColor;
        /// <summary>List of piece sets</summary>
        private readonly SortedList<string,PieceSet>         m_listPieceSet;
        /// <summary>Currently selected piece set</summary>
        private PieceSet?                                    m_pieceSet;
        /// <summary>Color use to create the background brush</summary>
        internal Color                                       m_colorBackground;
        /// <summary>Dispatcher timer</summary>
        private readonly DispatcherTimer                     m_dispatcherTimer;
        /// <summary>Current message mode</summary>
        private MessageMode                                  m_messageMode;
        /// <summary>Connection to FICS Chess Server</summary>
        private FicsInterface.FicsConnection?                m_ficsConnection;
        /// <summary>Setting to connect to the FICS server</summary>
        private readonly FicsInterface.FicsConnectionSetting m_ficsConnectionSetting;
        /// <summary>Convert properties settings to/from object setting</summary>
        private readonly SettingAdaptor                      m_settingAdaptor;
        /// <summary>Search criteria to use to find FICS game</summary>
        private FicsInterface.SearchCriteria                 m_searchCriteria;
        /// <summary>Index of the puzzle game being played (if not -1)</summary>
        private int                                          m_puzzleGameIndex;
        /// <summary>Mask of puzzle which has been solved</summary>
        internal long[]                                      m_puzzleMasks;
        #endregion

        #region Ctor
        /// <summary>
        /// Static Ctor
        /// </summary>
        static MainWindow() {
            NewGameCommand.InputGestures.Add(            new KeyGesture(Key.N,  ModifierKeys.Control));
            LoadGameCommand.InputGestures.Add(           new KeyGesture(Key.O,  ModifierKeys.Control));
            LoadPuzzleCommand.InputGestures.Add(         new KeyGesture(Key.U,  ModifierKeys.Control));
            SaveGameCommand.InputGestures.Add(           new KeyGesture(Key.S,  ModifierKeys.Control));
            QuitCommand.InputGestures.Add(               new KeyGesture(Key.F4, ModifierKeys.Alt));
            HintCommand.InputGestures.Add(               new KeyGesture(Key.H,  ModifierKeys.Control));
            UndoCommand.InputGestures.Add(               new KeyGesture(Key.Z,  ModifierKeys.Control));
            RedoCommand.InputGestures.Add(               new KeyGesture(Key.Y,  ModifierKeys.Control));
            RefreshCommand.InputGestures.Add(            new KeyGesture(Key.F5));
            SelectPlayersCommand.InputGestures.Add(      new KeyGesture(Key.P,  ModifierKeys.Control));
            AutomaticPlayCommand.InputGestures.Add(      new KeyGesture(Key.F2, ModifierKeys.Control));
            FastAutomaticPlayCommand.InputGestures.Add(  new KeyGesture(Key.F3, ModifierKeys.Control));
            ReversedBoardCommand.InputGestures.Add(      new KeyGesture(Key.R,  ModifierKeys.Control));
            CancelPlayCommand.InputGestures.Add(         new KeyGesture(Key.C,  ModifierKeys.Control));
            DesignModeCommand.InputGestures.Add(         new KeyGesture(Key.D,  ModifierKeys.Control));
            ManualSearchSettingCommand.InputGestures.Add(new KeyGesture(Key.M,  ModifierKeys.Control));
        }
        public MainWindow() {
            ExecutedRoutedEventHandler   onExecutedCmd;
            CanExecuteRoutedEventHandler onCanExecuteCmd;

            InitializeComponent();
            m_settingAdaptor                   = new SettingAdaptor(Properties.Settings.Default);
            m_listPieceSet                     = PieceSetStandard.LoadPieceSetFromResource();
            m_chessCtl.ParentBoardWindow       = this;
            m_messageMode                      = MessageMode.CallEndGame;
            m_lostPieceBlack.ChessBoardControl = m_chessCtl;
            m_lostPieceBlack.Color             = true;
            m_lostPieceWhite.ChessBoardControl = m_chessCtl;
            m_lostPieceWhite.Color             = false;
            m_ficsConnectionSetting            = new FicsInterface.FicsConnectionSetting("", -1, "");
            m_searchCriteria                   = new FicsInterface.SearchCriteria();
            m_puzzleMasks                      = new long[2];
            m_puzzleGameIndex                  = -1;
            m_settingAdaptor.LoadChessBoardCtl(m_chessCtl);
            m_settingAdaptor.LoadMainWindow(this, m_listPieceSet);
            m_settingAdaptor.LoadFicsConnectionSetting(m_ficsConnectionSetting);
            m_settingAdaptor.LoadFICSSearchCriteria(m_searchCriteria!);
            m_chessCtl.UpdateCmdState         += ChessCtl_UpdateCmdState;
            PlayingMode = MainPlayingMode.PlayerAgainstPlayer;
            m_chessCtl.MoveSelected           += ChessCtl_MoveSelected;
            m_chessCtl.QueryPiece             += ChessCtl_QueryPiece;
            m_chessCtl.QueryPawnPromotionType += ChessCtl_QueryPawnPromotionType;
            m_dispatcherTimer                  = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, new EventHandler(DispatcherTimer_Tick), Dispatcher);
            m_dispatcherTimer.Start();
            SetCmdState();
            mnuOptionFlashPiece.IsChecked      = m_chessCtl.MoveFlashing;
            mnuOptionsReversedBoard.IsChecked  = m_chessCtl.IsBoardReversed;
            m_ficsConnection                   = null;
            onExecutedCmd                      = new ExecutedRoutedEventHandler(OnExecutedCmd);
            onCanExecuteCmd                    = new CanExecuteRoutedEventHandler(OnCanExecuteCmd);
            foreach (RoutedUICommand cmd in m_arrCommands) {
                CommandBindings.Add(new CommandBinding(cmd, onExecutedCmd, onCanExecuteCmd));
            }
        }

        #endregion

        #region Command Handling
        public virtual void OnExecutedCmd(object sender, ExecutedRoutedEventArgs e) {
            ChessBoard.PlayerColor  computerColor;
            bool                    isPlayerAgainstPlayer;

            if (e.Command == QuitCommand) {
                Close();
            } else if (e.Command == FlashPieceCommand) {
                ToggleFlashPiece();
            } else if (e.Command == ReversedBoardCommand) {
                ToggleReversedBoard();
            } else if (e.Command == BoardSettingCommand) {
                ChooseBoardSetting();
            } else {
                e.Handled   = false;
            }
        }
        public virtual void OnCanExecuteCmd(object sender, CanExecuteRoutedEventArgs e) {
            bool isDesignMode;
            bool isBusy;
            bool isObservingGame;

            isDesignMode       = (PlayingMode == MainPlayingMode.DesignMode);
            isBusy             = m_chessCtl.IsBusy;
            isObservingGame    = m_chessCtl.IsObservingAGame;
            if (e.Command == NewGameCommand) {
                e.CanExecute = !(isDesignMode || isBusy || isObservingGame);
            } else if (e.Command == ReversedBoardCommand) {
                e.CanExecute = !(isBusy || isObservingGame || m_chessCtl.SelectedCell != -1);
            } else if (e.Command == QuitCommand                ||
                       e.Command == ManualSearchSettingCommand ||
                       e.Command == FlashPieceCommand          ||
                       e.Command == DesignModeCommand          ||
                       e.Command == ReversedBoardCommand       ||
                       e.Command == PgnNotationCommand         ||
                       e.Command == BoardSettingCommand) {
                e.CanExecute = !(isBusy || isObservingGame);
            } else {
                e.Handled = false;
            }
        }
        #endregion

        #region Properties
        public PieceSet? PieceSet {
            get => m_pieceSet;
            set {
                if (m_pieceSet != value) {
                    m_pieceSet                = value;
                    m_chessCtl.PieceSet       = value;
                    m_lostPieceBlack.PieceSet = value;
                    m_lostPieceWhite.PieceSet = value;
                }
            }
        }

        public MainPlayingMode PlayingMode {
            get => m_playingMode;
            set {
                m_playingMode = value;
                switch(m_playingMode) {
                case MainPlayingMode.PlayerAgainstPlayer:
                    m_chessCtl.WhitePlayerType = PgnPlayerType.Human;
                    m_chessCtl.BlackPlayerType = PgnPlayerType.Human;
                    break;
                case MainPlayingMode.ComputerPlayWhite:
                    m_chessCtl.WhitePlayerType = PgnPlayerType.Program;
                    m_chessCtl.BlackPlayerType = PgnPlayerType.Human;
                    break;
                case MainPlayingMode.ComputerPlayBlack:
                    m_chessCtl.WhitePlayerType = PgnPlayerType.Human;
                    m_chessCtl.BlackPlayerType = PgnPlayerType.Program;
                    break;
                default:
                    m_chessCtl.WhitePlayerType = PgnPlayerType.Program;
                    m_chessCtl.BlackPlayerType = PgnPlayerType.Program;
                    break;
                }
            }
        }

        public bool IsComputerMustPlay {
            get {
                bool                  retVal;
                ChessBoard.GameResult moveResult = ChessBoard.GameResult.OnGoing;
                ChessBoard            board;

                board   = m_chessCtl.Board;
                retVal  = m_playingMode switch {
                    MainPlayingMode.PlayerAgainstPlayer => false,
                    MainPlayingMode.ComputerPlayWhite   => (board.CurrentPlayer == ChessBoard.PlayerColor.White),
                    MainPlayingMode.ComputerPlayBlack   => (board.CurrentPlayer == ChessBoard.PlayerColor.Black),
                    MainPlayingMode.ComputerPlayBoth    => false,
                    _                                   => false,
                };
                if (retVal) {
                    retVal     = (moveResult == ChessBoard.GameResult.OnGoing || moveResult == ChessBoard.GameResult.Check);
                }
                return retVal;
            }
        }
        #endregion

        #region Methods

        private bool DisplayMessage(ChessBoard.GameResult moveResult, MessageMode messageMode) {
            bool                   retVal;
            ChessBoard.PlayerColor currentPlayerColor;
            string                 opponent;
            string?                msg = null;
            
            currentPlayerColor = m_chessCtl.ChessBoard.CurrentPlayer;
            opponent = m_playingMode switch {
                MainPlayingMode.ComputerPlayWhite => (currentPlayerColor == ChessBoard.PlayerColor.White) ? "Computer is" : "You are",
                MainPlayingMode.ComputerPlayBlack => (currentPlayerColor == ChessBoard.PlayerColor.Black) ? "Computer is" : "You are",
                _                                 => (currentPlayerColor == ChessBoard.PlayerColor.White) ? "White player is" : "Black player is",
            };
            switch (moveResult) {
            case ChessBoard.GameResult.OnGoing:
                retVal = false;
                break;
            case ChessBoard.GameResult.TieNoMove:
                m_chessCtl.GameTimer.Enabled = false;
                msg    = $"Draw. {opponent} unable to move.";
                retVal = true;
                break;
            case ChessBoard.GameResult.TieNoMatePossible:
                msg    = "Draw. Not enough pieces to make a checkmate.";
                retVal = true;
                break;
            case ChessBoard.GameResult.ThreeFoldRepeat:
                msg    = "Draw. 3 times the same board.";
                retVal = true;
                break;
            case ChessBoard.GameResult.FiftyRuleRepeat:
                msg    = "Draw. 50 moves without moving a pawn or eating a piece.";
                retVal = true;
                break;
            case ChessBoard.GameResult.Check:
                if (messageMode == MessageMode.Verbose) {
                    msg = $"{opponent} in check.";
                }
                if (m_puzzleGameIndex != -1) {
                    m_puzzleMasks[m_puzzleGameIndex / 64] |= 1L << (m_puzzleGameIndex & 63);
                }
                retVal = false;
                break;
            case ChessBoard.GameResult.Mate:
                msg    = $"{opponent} checkmate.";
                retVal = true;
                break;
            default:
                retVal = false;
                break;
            }
            if (retVal) {
                m_chessCtl.GameTimer.Enabled = false;
            }
            if (messageMode != MessageMode.Silent && msg != null) {
                MessageBox.Show(msg);
            }
            return retVal;
        }
        private void ResetBoard() {
            m_chessCtl.ResetBoard();
            SetCmdState();
        }

        public static void SetCmdState() => CommandManager.InvalidateRequerySuggested();

        private void UnlockBoard() {
            Cursor = Cursors.Arrow;
            SetCmdState();
        }

        private void CloseFicsIfConnected() {
            if (m_ficsConnection != null) {
                DisconnectFromFics();
            }
        }

        private void SetFicsHeader(FicsInterface.FicsGame? game) {
            if (game != null) {
                m_toolbar.labelWhitePlayerName.Content       = $"({game.WhitePlayerName}) :";
                m_toolbar.labelWhitePlayerName.ToolTip       = $"Rating = {FicsInterface.FicsGame.GetHumanRating(game.WhiteRating)}";
                m_toolbar.labelWhitePlayTime.Content         = "???";
                m_toolbar.labelWhiteLimitPlayTime.Visibility = Visibility.Visible;
                m_toolbar.labelBlackPlayerName.Content       = $"({game.BlackPlayerName}) :";
                m_toolbar.labelBlackPlayerName.ToolTip       = $"Rating = {FicsInterface.FicsGame.GetHumanRating(game.BlackRating)}";
                m_toolbar.labelBlackPlayTime.Content         = "???";
                m_toolbar.labelBlackLimitPlayTime.Visibility = Visibility.Visible;
            } else {
                m_toolbar.labelWhitePlayerName.Content       = "";
                m_toolbar.labelWhitePlayerName.ToolTip       = "";
                m_toolbar.labelWhitePlayTime.Content         = "";
                m_toolbar.labelWhiteLimitPlayTime.Visibility = Visibility.Collapsed;
                m_toolbar.labelBlackPlayerName.Content       = "";
                m_toolbar.labelBlackPlayerName.ToolTip       = "";
                m_toolbar.labelBlackLimitPlayTime.Content    = "";
                m_toolbar.labelBlackPlayTime.Content         = "";
                m_toolbar.labelBlackLimitPlayTime.Visibility = Visibility.Collapsed;
            }
        }

        //not used
        private void DisconnectFromFics() {
            if (m_ficsConnection != null) {
                m_ficsConnection.Dispose();
                m_ficsConnection = null;
                SetFicsHeader(game: null);
            }
        }


        private void ToggleFlashPiece() => m_chessCtl.MoveFlashing = mnuOptionFlashPiece.IsChecked;

        private void ToggleReversedBoard() {
            m_chessCtl.IsBoardReversed        = !m_chessCtl.IsBoardReversed;
            mnuOptionsReversedBoard.IsChecked = m_chessCtl.IsBoardReversed;
        }

        private void ChooseBoardSetting() {
            FrmBoardSetting frm;

            frm = new FrmBoardSetting(m_chessCtl.LiteCellColor,
                                      m_chessCtl.DarkCellColor,
                                      m_chessCtl.WhitePieceColor,
                                      m_chessCtl.BlackPieceColor,
                                      m_colorBackground,
                                      m_listPieceSet,
                                      PieceSet!) {
                Owner = this
            };
            if (frm.ShowDialog() == true) {
                m_colorBackground          = frm.BackgroundColor;
                Background                 = new SolidColorBrush(m_colorBackground);
                m_chessCtl.LiteCellColor   = frm.LiteCellColor;
                m_chessCtl.DarkCellColor   = frm.DarkCellColor;
                m_chessCtl.WhitePieceColor = frm.WhitePieceColor;
                m_chessCtl.BlackPieceColor = frm.BlackPieceColor;
                PieceSet                   = frm.PieceSet;
            }
        }
        #endregion

        #region Sink
        private void DispatcherTimer_Tick(object? sender, EventArgs e) {
            GameTimer gameTimer;
            
            gameTimer                            = m_chessCtl.GameTimer;
            m_toolbar.labelWhitePlayTime.Content = GameTimer.GetHumanElapse(gameTimer.WhitePlayTime);
            m_toolbar.labelBlackPlayTime.Content = GameTimer.GetHumanElapse(gameTimer.BlackPlayTime);
            if (gameTimer.MaxWhitePlayTime.HasValue) {
                m_toolbar.labelWhiteLimitPlayTime.Content = $"({GameTimer.GetHumanElapse(gameTimer.MaxWhitePlayTime.Value)}/{gameTimer.MoveIncInSec})";
            }
            if (gameTimer.MaxBlackPlayTime.HasValue) {
                m_toolbar.labelBlackLimitPlayTime.Content = $"({GameTimer.GetHumanElapse(gameTimer.MaxBlackPlayTime.Value)}/{gameTimer.MoveIncInSec})";
            }
        }

        private void ChessCtl_QueryPiece(object sender, ChessBoardControl.QueryPieceEventArgs e) => e.PieceType = m_lostPieceBlack.SelectedPiece;

        private void ChessCtl_QueryPawnPromotionType(object sender, ChessBoardControl.QueryPawnPromotionTypeEventArgs e) {
            FrmQueryPawnPromotionType frm;

            frm = new FrmQueryPawnPromotionType(e.ValidPawnPromotion) {
                Owner = this
            };
            frm.ShowDialog();
            e.PawnPromotionType = frm.PromotionType;
        }

        void ChessCtl_MoveSelected(object? sender, ChessBoardControl.MoveSelectedEventArgs e) => m_chessCtl.DoUserMove(e.Move);

        private void ChessCtl_UpdateCmdState(object? sender, EventArgs e) {
            m_lostPieceBlack.Refresh();
            m_lostPieceWhite.Refresh();
            SetCmdState();
        }
        #endregion

    }
}
