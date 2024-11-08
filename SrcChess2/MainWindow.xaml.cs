﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GenericSearchEngine;
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
            public  ChessSearchSetting?  OriSearchMode { get; set; }
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
        /// <summary>Utility class to handle board evaluation objects</summary>
        private readonly BoardEvaluationUtil                 m_boardEvalUtil;
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
        /// <summary>Search mode</summary>
        private readonly ChessSearchSetting                  m_chessSearchSetting;
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
            m_moveViewer.ChessControl          = m_chessCtl;
            m_messageMode                      = MessageMode.CallEndGame;
            m_lostPieceBlack.ChessBoardControl = m_chessCtl;
            m_lostPieceBlack.Color             = true;
            m_lostPieceWhite.ChessBoardControl = m_chessCtl;
            m_lostPieceWhite.Color             = false;
            m_ficsConnectionSetting            = new FicsInterface.FicsConnectionSetting("", -1, "");
            m_boardEvalUtil                    = new BoardEvaluationUtil();
            m_searchCriteria                   = new FicsInterface.SearchCriteria();
            m_puzzleMasks                      = new long[2];
            m_puzzleGameIndex                  = -1;
            m_settingAdaptor.LoadChessBoardCtl(m_chessCtl);
            m_settingAdaptor.LoadMainWindow(this, m_listPieceSet);
            m_settingAdaptor.LoadFicsConnectionSetting(m_ficsConnectionSetting);
            m_settingAdaptor.LoadSearchMode(m_boardEvalUtil, out m_chessSearchSetting);
            m_settingAdaptor.LoadMoveViewer(m_moveViewer);
            m_settingAdaptor.LoadFICSSearchCriteria(m_searchCriteria!);
            m_chessCtl.ChessSearchSetting      = m_chessSearchSetting.GetBoardSearchSetting();
            m_chessCtl.UpdateCmdState         += ChessCtl_UpdateCmdState;
            PlayingMode                        = MainPlayingMode.ComputerPlayBlack;
            m_moveViewer.NewMoveSelected      += MoveViewer_NewMoveSelected;
            m_chessCtl.MoveSelected           += ChessCtl_MoveSelected;
            m_chessCtl.NewMove                += ChessCtl_NewMove;
            m_chessCtl.QueryPiece             += ChessCtl_QueryPiece;
            m_chessCtl.QueryPawnPromotionType += ChessCtl_QueryPawnPromotionType;
            m_chessCtl.FindMoveBegin          += ChessCtl_FindMoveBegin;
            m_chessCtl.FindMoveEnd            += ChessCtl_FindMoveEnd;
            m_dispatcherTimer                  = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, new EventHandler(DispatcherTimer_Tick), Dispatcher);
            m_dispatcherTimer.Start();
            SetCmdState();
            ShowSearchMode();
            mnuOptionFlashPiece.IsChecked      = m_chessCtl.MoveFlashing;
            mnuOptionsReversedBoard.IsChecked  = m_chessCtl.IsBoardReversed;
            m_ficsConnection                   = null;
            onExecutedCmd                      = new ExecutedRoutedEventHandler(OnExecutedCmd);
            onCanExecuteCmd                    = new CanExecuteRoutedEventHandler(OnCanExecuteCmd);
            Closing                           += MainWindow_Closing;
            Closed                            += MainWindow_Closed;
            foreach (RoutedUICommand cmd in m_arrCommands) {
                CommandBindings.Add(new CommandBinding(cmd, onExecutedCmd, onCanExecuteCmd));
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e) {
            if (CheckIfDirty()) {
                e.Cancel = true;
            }
        }
        private void MainWindow_Closed(object? sender, EventArgs e) {
            m_settingAdaptor.SaveChessBoardCtl(m_chessCtl);
            m_settingAdaptor.SaveMainWindow(this);
            m_settingAdaptor.SaveFicsConnectionSetting(m_ficsConnectionSetting);
            m_settingAdaptor.SaveSearchMode(m_chessSearchSetting);
            m_settingAdaptor.SaveMoveViewer(m_moveViewer);
            m_settingAdaptor.SaveFicsSearchCriteria(m_searchCriteria);
            m_settingAdaptor.Settings.Save();
            if (m_ficsConnection != null) {
                m_ficsConnection.Dispose();
                m_ficsConnection = null;
            }
        }

        #endregion

        #region Command Handling
        public virtual void OnExecutedCmd(object sender, ExecutedRoutedEventArgs e) {
            ChessBoard.PlayerColor  computerColor;
            bool                    isPlayerAgainstPlayer;

            if (e.Command == NewGameCommand) {
                NewGame();
            } else if (e.Command == LoadPuzzleCommand) {
                LoadPuzzle();
            } else if (e.Command == QuitCommand) {
                Close();
            } else if (e.Command == HintCommand) {
                ShowHint();
            } else if (e.Command == UndoCommand) {
                isPlayerAgainstPlayer = PlayingMode == MainPlayingMode.PlayerAgainstPlayer;
                computerColor = PlayingMode == MainPlayingMode.ComputerPlayWhite ? ChessBoard.PlayerColor.White : ChessBoard.PlayerColor.Black;
                m_chessCtl.UndoMove(isPlayerAgainstPlayer, computerColor);
            } else if (e.Command == RedoCommand) {
                m_chessCtl.RedoMove(PlayingMode == MainPlayingMode.PlayerAgainstPlayer);
            } else if (e.Command == RefreshCommand) {
                m_chessCtl.Refresh();
            } else if (e.Command == SelectPlayersCommand) {
                SelectPlayers();
            } else if (e.Command == AutomaticPlayCommand) {
                PlayComputerAgainstComputer(true);
            } else if (e.Command == FastAutomaticPlayCommand) {
                PlayComputerAgainstComputer(false);
            } else if (e.Command == CancelPlayCommand) {
                CancelAutoPlay();
            } else if (e.Command == DesignModeCommand) {
                ToggleDesignMode();
            } else if (e.Command == ManualSearchSettingCommand) {
                ShowManualSearchSetting();
            } else if (e.Command == FlashPieceCommand) {
                ToggleFlashPiece();
            } else if (e.Command == ReversedBoardCommand) {
                ToggleReversedBoard();
            } else if (e.Command == BoardSettingCommand) {
                ChooseBoardSetting();
            } else if (e.Command == TestBoardEvaluationCommand) {
                TestBoardEvaluation();
            } else {
                e.Handled   = false;
            }
        }
        public virtual void OnCanExecuteCmd(object sender, CanExecuteRoutedEventArgs e) {
            bool isDesignMode;
            bool isBusy;
            bool isSearchEngineBusy;
            bool isObservingGame;

            isDesignMode       = (PlayingMode == MainPlayingMode.DesignMode);
            isBusy             = m_chessCtl.IsBusy;
            isSearchEngineBusy = ChessBoardControl.IsSearchEngineBusy;
            isObservingGame    = m_chessCtl.IsObservingAGame;
            if (e.Command == NewGameCommand           ||
                e.Command == CreateGameCommand        ||
                e.Command == LoadGameCommand          ||
                e.Command == LoadPuzzleCommand        ||
                e.Command == SaveGameCommand          ||
                e.Command == HintCommand              ||
                e.Command == RefreshCommand           ||
                e.Command == SelectPlayersCommand     ||
                e.Command == AutomaticPlayCommand     ||
                e.Command == FastAutomaticPlayCommand) {
                e.CanExecute = !(isSearchEngineBusy || isDesignMode || isBusy || isObservingGame);
            } else if (e.Command == ReversedBoardCommand) {
                e.CanExecute = !(isSearchEngineBusy || isBusy || isObservingGame || m_chessCtl.SelectedCell != -1);
            } else if (e.Command == QuitCommand                ||
                       e.Command == ManualSearchSettingCommand ||
                       e.Command == FlashPieceCommand          ||
                       e.Command == DesignModeCommand          ||
                       e.Command == ReversedBoardCommand       ||
                       e.Command == PgnNotationCommand         ||
                       e.Command == BoardSettingCommand        ||
                       e.Command == TestBoardEvaluationCommand) {
                e.CanExecute = !(isSearchEngineBusy || isBusy || isObservingGame);
            } else if (e.Command == CancelPlayCommand) {
                e.CanExecute = isSearchEngineBusy | isBusy | isObservingGame;
            } else if (e.Command == UndoCommand) {
                e.CanExecute = (!isSearchEngineBusy && !isBusy && !isObservingGame && !isDesignMode && m_chessCtl.UndoCount >= ((m_playingMode == MainPlayingMode.PlayerAgainstPlayer) ? 1 : 2));
            } else if (e.Command == RedoCommand) {
                e.CanExecute = (!isSearchEngineBusy && !isBusy && !isObservingGame && !isDesignMode && m_chessCtl.RedoCount != 0);
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
                ChessBoard.GameResult moveResult;
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
                    moveResult = board.GetCurrentResult();
                    retVal     = (moveResult == ChessBoard.GameResult.OnGoing || moveResult == ChessBoard.GameResult.Check);
                }
                return retVal;
            }
        }

        public static int GetSearchThreadRunningCount => SearchEngine<ChessGameBoardAdaptor, Move>.GetRunningThreadCount();
        #endregion

        #region Methods
        private bool CheckIfDirty() {
            bool retVal = false;

            if (m_chessCtl.IsDirty) {
                switch(MessageBox.Show("Board has been changed. Do you want to save it?", "SrcChess2", MessageBoxButton.YesNoCancel)) {
                case MessageBoxResult.Yes:
                    if (!m_chessCtl.SaveToFile()) {
                        retVal = true;
                    }
                    break;
                case MessageBoxResult.No:
                    break;
                case MessageBoxResult.Cancel:
                    retVal = true;
                    break;
                }
            }
            return retVal;
        }

        private void UpdateFindingBestMoveStatus(int threadCount) => m_statusLabelMove.Content = $"Finding Best Move: {threadCount} thread{(threadCount > 1 ? "s" : "")} running...";

        private void StartAsyncComputing() {
            bool                isDifferentThreadForUI;
            ChessSearchSetting  chessSearchSetting;

            chessSearchSetting = m_chessCtl.ChessSearchSetting;
            if (chessSearchSetting.ThreadingMode == ThreadingMode.OnePerProcessorForSearch) {
                isDifferentThreadForUI = true;
            } else if (chessSearchSetting.ThreadingMode == ThreadingMode.DifferentThreadForSearch) {
                isDifferentThreadForUI = true;
            } else {
                isDifferentThreadForUI = false;
            }
            if (isDifferentThreadForUI) {
                SetCmdState();
            }
            UpdateFindingBestMoveStatus(threadCount: 0);
            m_statusLabelPermutation.Content = "";
            Cursor                           = Cursors.Wait;
        }

        private void ShowMoveInStatusBar(ChessBoard.PlayerColor playerColor, MoveExt move) {
            string                           permCount;
            System.Globalization.CultureInfo ci;

            if (m_chessCtl.IsObservingAGame) {
                permCount = "Waiting next move...";
            } else {
                ci = new System.Globalization.CultureInfo("en-US");
                permCount = move.PermutationCount switch {
                    -1  => "Found in Book.",
                    _   => $"{move.PermutationCount.ToString("C0", ci).Replace("$", "")} permutations evaluated. {move.CacheHit.ToString("C0", ci).Replace("$", "")} found in cache.",
                };
                if (move.SearchDepth != -1) {
                    permCount += $" {move.SearchDepth} ply.";
                }
                permCount += $" {move.TimeToCompute.TotalSeconds} sec(s).";
            }
            m_statusLabelMove.Content        = $"{((playerColor == ChessBoard.PlayerColor.Black) ? "Black " : "White ")}{move.GetHumanPos()}";
            m_statusLabelPermutation.Content = permCount;
        }

        private void ShowSearchMode() {
            string  searchModeTxt;

            searchModeTxt = m_chessSearchSetting.DifficultyLevel switch {
                ChessSearchSetting.SettingDifficultyLevel.Manual       => "Manual",
                ChessSearchSetting.SettingDifficultyLevel.VeryEasy     => "Beginner",
                ChessSearchSetting.SettingDifficultyLevel.Easy         => "Easy",
                ChessSearchSetting.SettingDifficultyLevel.Intermediate => "Intermediate",
                ChessSearchSetting.SettingDifficultyLevel.Hard         => "Advanced",
                ChessSearchSetting.SettingDifficultyLevel.VeryHard     => "More advanced",
                _                                                      => "???",
            };
            m_statusLabelSearchMode.Content = searchModeTxt;
            m_statusLabelSearchMode.ToolTip = m_chessSearchSetting.GetBoardSearchSetting().HumanSearchMode();
        }
        public void SetSearchMode(ChessSearchSetting chessSearchSetting) {
            m_chessCtl.ChessSearchSetting = chessSearchSetting;
            ShowSearchMode();
        }

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

        private void PlayComputerEnd(bool flash, object? move) {
            ChessBoard.GameResult  result;

            if (move is MoveExt moveExt) { 
                result = m_chessCtl.DoMove(moveExt, flash);
                switch(m_playingMode) {
                case MainPlayingMode.ComputerPlayBoth:
                    switch (result) {
                    case ChessBoard.GameResult.OnGoing:
                    case ChessBoard.GameResult.Check:
                        PlayComputer(flash);
                        break;
                    case ChessBoard.GameResult.ThreeFoldRepeat:
                    case ChessBoard.GameResult.FiftyRuleRepeat:
                    case ChessBoard.GameResult.TieNoMove:
                    case ChessBoard.GameResult.TieNoMatePossible:
                    case ChessBoard.GameResult.Mate:
                        break;
                    default:
                        break;
                    }
                    break;
                }
            }
            UnlockBoard();
        }

        private void PlayComputer(bool flash) {
            StartAsyncComputing();
            if (!m_chessCtl.FindBestMove(null, (x,y) => PlayComputerEnd(x, y), flash)) {
                UnlockBoard();
            }
        }
        private void PlayComputerAgainstComputer(bool flash) {
            m_playingMode = MainPlayingMode.ComputerPlayBoth;
            PlayComputer(flash);
        }

        private void TestShowResult(BoardEvaluationStat stat) {
            string             msg;
            string             method1;
            string             method2;
            ChessSearchSetting chessSearchSetting;

            chessSearchSetting = m_chessCtl.ChessSearchSetting;
            method1            = chessSearchSetting.WhiteBoardEvaluator.Name;
            method2            = chessSearchSetting.BlackBoardEvaluator.Name;
            msg                = $"{stat.GameCount} game(s) played.\r\n" +
                                 $"{stat.Method1WinCount} win(s) for method #1 ({method1}). Average time = {stat.Method1WinCount} ms per move.\r\n" + 
                                 $"{stat.Method2WinCount} win(s) for method #2 ({method2}). Average time = {stat.Method2WinCount} ms per move.\r\n" + 
                                 $"({stat.GameCount - stat.Method1WinCount - stat.Method2WinCount} draw(s).";
            MessageBox.Show(msg);
        }

        private void TestBoardEvaluation_StartNewGame(BoardEvaluationStat stat) {
            ChessSearchSetting chessSearchSetting;
            IBoardEvaluation   boardEvaluation;

            m_chessCtl.ResetBoard();
            chessSearchSetting                     = m_chessCtl.ChessSearchSetting;
            boardEvaluation                        = chessSearchSetting.WhiteBoardEvaluator;
            chessSearchSetting.WhiteBoardEvaluator = chessSearchSetting.BlackBoardEvaluator;
            chessSearchSetting.BlackBoardEvaluator = boardEvaluation;
            if (!m_chessCtl.FindBestMove(null, (x,y) => TestBoardEvaluation_PlayNextMove(x, y), stat)) {
                throw new ApplicationException("How did we get here?");
            }
        }

        private void TestBoardEvaluation_PlayNextMove(BoardEvaluationStat stat, object? move) {
            ChessBoard.GameResult result;
            bool                  isSearchCancel;
            bool                  isEven;

            isEven         = ((stat.GameIndex & 1) == 0);
            isSearchCancel = ChessBoardControl.IsSearchCancel;
            if (move is not MoveExt moveExt || isSearchCancel) {
                result = ChessBoard.GameResult.TieNoMove;
            } else if (m_chessCtl.Board.MovePosStack.Count > 250) {
                result = ChessBoard.GameResult.TieNoMatePossible;
            } else {
                if ((m_chessCtl.Board.CurrentPlayer == ChessBoard.PlayerColor.White && isEven) ||
                    (m_chessCtl.Board.CurrentPlayer == ChessBoard.PlayerColor.Black && !isEven)) {
                    stat.Method1Time += moveExt.TimeToCompute;
                    stat.Method1MoveCount++;
                } else {
                    stat.Method2Time += moveExt.TimeToCompute;
                    stat.Method2MoveCount++;
                }
                result = m_chessCtl.DoMove(moveExt, flash: false);
            }
            if (result == ChessBoard.GameResult.OnGoing || result == ChessBoard.GameResult.Check) {
                if (!m_chessCtl.FindBestMove(null, (x,y) => TestBoardEvaluation_PlayNextMove(x, y), stat)) {
                    throw new ApplicationException("How did we get here?");
                }
            } else {
                if (result == ChessBoard.GameResult.Mate) {
                    if ((m_chessCtl.NextMoveColor == ChessBoard.PlayerColor.Black && isEven) ||
                        (m_chessCtl.NextMoveColor == ChessBoard.PlayerColor.White && !isEven)) {
                        stat.Method1WinCount++;
                    } else {
                        stat.Method2WinCount++;
                    }
                }
                stat.GameIndex++;
                if (stat.GameIndex < stat.GameCount && !isSearchCancel) {
                    TestBoardEvaluation_StartNewGame(stat);
                } else {
                    TestShowResult(stat);
                    PlayingMode                   = MainPlayingMode.PlayerAgainstPlayer;
                    m_chessCtl.ChessSearchSetting = stat.OriSearchMode!;
                    m_messageMode                 = stat.OriMessageMode;
                    UnlockBoard();
                }
            }
        }

        private void TestBoardEvaluation(int gameCount, ChessSearchSetting chessSearchSetting) {
            BoardEvaluationStat stat;

            stat = new BoardEvaluationStat(gameCount) {
                OriSearchMode  = m_chessCtl.ChessSearchSetting,
                OriMessageMode = m_messageMode
            };
            m_messageMode                 = MessageMode.Silent;
            m_chessCtl.ChessSearchSetting = chessSearchSetting;
            PlayingMode                   = MainPlayingMode.TestEvaluationMethod;
            TestBoardEvaluation_StartNewGame(stat);
        }

        private void ShowHintEnd(bool isBeforeMove, object? move) {
            if (isBeforeMove && move is MoveExt moveExt) {
                ShowMoveInStatusBar(m_chessCtl.NextMoveColor, moveExt);
            } else {
                UnlockBoard();
            }
        }
        private void ShowHint() {
            m_puzzleGameIndex = -1; // Hint means you didn't solve it by yourself
            StartAsyncComputing();
            if (!m_chessCtl.ShowHint((x,y) => ShowHintEnd(x,y))) {
                UnlockBoard();
            }
        }

        private void ToggleDesignMode() {
            if (PlayingMode == MainPlayingMode.DesignMode || MessageBox.Show(this, " Move list will be lost. Do you still want to switch to design mode?", "Design mode", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                if (PlayingMode == MainPlayingMode.DesignMode) {
                    PlayingMode                   = MainPlayingMode.PlayerAgainstPlayer;
                    mnuEditDesignMode.IsCheckable = false;
                    if (FrmGameParameter.AskGameParameter(this, m_chessSearchSetting, m_boardEvalUtil)) {
                        ShowSearchMode();
                        m_chessCtl.BoardDesignMode = false;
                        if (m_chessCtl.BoardDesignMode) {
                            PlayingMode = MainPlayingMode.DesignMode;
                            MessageBox.Show("Invalid board configuration. Correct or reset.");
                        } else {
                            m_lostPieceBlack.BoardDesignMode = false;
                            m_lostPieceWhite.Visibility      = Visibility.Visible;
                            StartAutomaticMove();
                        }
                    } else {
                        PlayingMode = MainPlayingMode.DesignMode;
                    }
                } else {
                    PlayingMode                      = MainPlayingMode.DesignMode;
                    mnuEditDesignMode.IsCheckable    = true;
                    m_lostPieceBlack.BoardDesignMode = true;
                    m_lostPieceWhite.Visibility      = Visibility.Hidden;
                    m_chessCtl.BoardDesignMode       = true;
                }
                mnuEditDesignMode.IsChecked = (PlayingMode == MainPlayingMode.DesignMode);
                SetCmdState();
            }
        }

        private void CloseFicsIfConnected() {
            if (m_ficsConnection != null) {
                DisconnectFromFics();
            }
        }

        private void NewGame() {
            if (!CheckIfDirty()) {
                if (FrmGameParameter.AskGameParameter(this, m_chessSearchSetting, m_boardEvalUtil)) {
                    ShowSearchMode();
                    ResetBoard();
                    CloseFicsIfConnected();
                    StartAutomaticMove();
                }
            }
        }

        private void LoadPuzzle() {
            FrmLoadPuzzle frm;
            PgnGame       game;

            if (!CheckIfDirty()) {
                frm = new FrmLoadPuzzle(m_puzzleMasks) {
                    Owner = this
                };
                if (frm.ShowDialog() == true) {
                    CloseFicsIfConnected();
                    game = frm.Game;
                    m_chessCtl.CreateGameFromMove(game.StartingChessBoard,
                                                  [],
                                                  game.StartingColor,
                                                  "White",
                                                  "Black",
                                                  PgnPlayerType.Human,
                                                  PgnPlayerType.Program,
                                                  TimeSpan.Zero,
                                                  TimeSpan.Zero);
                    PlayingMode                      = MainPlayingMode.ComputerPlayBlack;
                    m_statusLabelPermutation.Content = game.Event;
                    m_puzzleGameIndex                = frm.GameIndex;
                    DoAutomaticMove();
                }
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

        private void CancelAutoPlay() {
            if (m_chessCtl.IsObservingAGame) {
                m_ficsConnection!.TerminateObservation(m_chessCtl);
            } else if (PlayingMode == MainPlayingMode.ComputerPlayBoth) {
                PlayingMode = MainPlayingMode.PlayerAgainstPlayer;
            } else {
                ChessBoardControl.CancelSearch();
            }
        }
        private void SelectPlayers() {
            if (FrmGameParameter.AskGameParameter(this, m_chessSearchSetting, m_boardEvalUtil)) {
                StartAutomaticMove();
            }
        }

        private void ShowManualSearchSetting() {
            FrmSearchMode frm;

            frm = new(m_chessSearchSetting, m_boardEvalUtil) {
                Owner = this
            };
            if (frm.ShowDialog() == true) {
                SetSearchMode(m_chessSearchSetting.GetBoardSearchSetting());
            }
        }

        private void TestBoardEvaluation() {
            FrmTestBoardEval    frm;
            ChessSearchSetting  chessSearchSetting;
            int                 gameCount;

            if (!CheckIfDirty()) {
                frm = new FrmTestBoardEval(m_boardEvalUtil, m_chessCtl.ChessSearchSetting) {
                    Owner = this
                };
                if (frm.ShowDialog() == true) {
                    CloseFicsIfConnected();
                    chessSearchSetting = frm.ChessSearchSetting;
                    gameCount          = frm.GameCount;
                    TestBoardEvaluation(gameCount, chessSearchSetting);
                }
            }
        }

        private void DoAutomaticMove(bool flashing) {
            if (IsComputerMustPlay) {
                PlayComputer(flashing);
            }
        }

        private void DoAutomaticMove() => DoAutomaticMove(m_chessCtl.MoveFlashing);

        private void StartAutomaticMove() {
            if (m_playingMode == MainPlayingMode.ComputerPlayBoth) {
                PlayComputerAgainstComputer(m_chessCtl.MoveFlashing);
            } else {
                DoAutomaticMove();
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
            int       threadCount = GetSearchThreadRunningCount;
            
            gameTimer                            = m_chessCtl.GameTimer;
            m_toolbar.labelWhitePlayTime.Content = GameTimer.GetHumanElapse(gameTimer.WhitePlayTime);
            m_toolbar.labelBlackPlayTime.Content = GameTimer.GetHumanElapse(gameTimer.BlackPlayTime);
            if (gameTimer.MaxWhitePlayTime.HasValue) {
                m_toolbar.labelWhiteLimitPlayTime.Content = $"({GameTimer.GetHumanElapse(gameTimer.MaxWhitePlayTime.Value)}/{gameTimer.MoveIncInSec})";
            }
            if (gameTimer.MaxBlackPlayTime.HasValue) {
                m_toolbar.labelBlackLimitPlayTime.Content = $"({GameTimer.GetHumanElapse(gameTimer.MaxBlackPlayTime.Value)}/{gameTimer.MoveIncInSec})";
            }
            if (threadCount > 0) {
                UpdateFindingBestMoveStatus(threadCount);
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

        private void ChessCtl_FindMoveBegin(object? sender, EventArgs e) => m_toolbar.StartProgressBar();
        private void ChessCtl_FindMoveEnd(object? sender, EventArgs e) => m_toolbar.EndProgressBar();
        private void ChessCtl_NewMove(object? sender, ChessBoardControl.NewMoveEventArgs e) {
            MoveExt                move;
            ChessBoard.PlayerColor moveColor;

            move      = e.Move;
            moveColor = m_chessCtl.ChessBoard.LastMovePlayer;
            ShowMoveInStatusBar(moveColor, move);
            DisplayMessage(e.MoveResult, m_messageMode);
            DoAutomaticMove();
        }

        private void MoveViewer_NewMoveSelected(object? sender, MoveViewer.NewMoveSelectedEventArg e) {
            ChessBoard.GameResult result;

            if (PlayingMode == MainPlayingMode.PlayerAgainstPlayer) {
                result = m_chessCtl.SelectMove(e.NewIndex, out bool succeed);
                DisplayMessage(result, MessageMode.Verbose);
                e.Cancel = !succeed;
            } else {
                e.Cancel = true;
            }
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
