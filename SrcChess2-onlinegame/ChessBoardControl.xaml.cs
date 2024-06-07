using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.ComponentModel;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Globalization;
using SrcChess2.Core;
using SrcChess2.PgnParsing;
using System.Diagnostics;
using System.Windows.Controls.Primitives;

namespace SrcChess2
{
    public partial class ChessBoardControl : UserControl {

        #region Inner Class
        public struct IntPoint(int x, int y) {
            public int X = x;
            public int Y = y;
        }
        public class NewMoveEventArgs(MoveExt move, ChessBoard.GameResult moveResult) : EventArgs {
            public MoveExt Move { get; private set; } = move;
            public ChessBoard.GameResult MoveResult { get; private set; } = moveResult;
        }
        public interface IUpdateCmd {
            void        Update();
        }
        private class SyncFlash(ChessBoardControl chessBoardControl, SolidColorBrush brush, Color colorStart, Color colorEnd) {
            private readonly ChessBoardControl  m_chessBoardControl = chessBoardControl;
            private readonly SolidColorBrush    m_brush = brush;
            private Color                       m_startColor = colorStart;
            private Color                       m_endColor = colorEnd;
            private DispatcherFrame?            m_dispatcherFrame;

            //Flash the specified cell
            private void FlashCell(int count, double sec, EventHandler eventHandlerTerminated) {
                ColorAnimation animationColor;

                animationColor = new ColorAnimation(m_startColor, m_endColor, new Duration(TimeSpan.FromSeconds(sec))) {
                    AutoReverse    = true,
                    RepeatBehavior = new RepeatBehavior(count / 2)
                };
                if (eventHandlerTerminated != null) {
                    animationColor.Completed += new EventHandler(eventHandlerTerminated);
                }
                m_brush.BeginAnimation(SolidColorBrush.ColorProperty, animationColor);
            }

            //show move
            public void Flash() {
                m_chessBoardControl.IsEnabled = false;
                FlashCell(4, 0.15, new EventHandler(FirstFlash_Completed));
                m_dispatcherFrame = new DispatcherFrame();
                Dispatcher.PushFrame(m_dispatcherFrame);
            }
            private void FirstFlash_Completed(object? sender, EventArgs e) {
                m_chessBoardControl.IsEnabled = true;
                m_dispatcherFrame!.Continue   = false;

            }
        }
        public class MoveSelectedEventArgs(MoveExt move) : EventArgs {
            public MoveExt Move = move;
        }
        public class QueryPieceEventArgs(int pos, ChessBoard.PieceType defPieceType) : EventArgs {
            public int                  Pos { get; private set; } = pos;
            public ChessBoard.PieceType PieceType { get; set; } = defPieceType;
        }
        public class QueryPawnPromotionTypeEventArgs(ChessBoard.ValidPawnPromotion validPawnPromotion) : EventArgs {
            //Promotion type (Queen, Rook, Bishop, Knight or Pawn)
            public Move.MoveType                 PawnPromotionType { get; set; } = Move.MoveType.Normal;
            //Possible pawn promotions in the current context
            public ChessBoard.ValidPawnPromotion ValidPawnPromotion { get; private set; } = validPawnPromotion;
        }
        private class FindBestMoveCookie<T>(Action<T, object?> moveFoundAction, T cookie) {
            //Action to trigger when the move is found
            public Action<T, object?> MoveFoundAction { get; private set; } = moveFoundAction;
            //Cookie to be used by the action
            public T                  Cookie { get; private set; } = cookie;        
            public DateTime           TimeSearchStarted { get; private set; } = DateTime.Now;
        }
        #endregion

        #region Members
        public static readonly DependencyProperty         LiteCellColorProperty;
        public static readonly DependencyProperty         DarkCellColorProperty;
        public static readonly DependencyProperty         WhitePieceColorProperty;
        public static readonly DependencyProperty         BlackPieceColorProperty;
        public static readonly  DependencyProperty        MoveFlashingProperty;
        public static readonly DependencyProperty         IsBoardReversedProperty;

        public event EventHandler<MoveSelectedEventArgs>? MoveSelected;
        public event EventHandler<EventArgs>?             BoardReset;
        public event EventHandler<NewMoveEventArgs>?      NewMove;
        public event EventHandler?                        RedoPosChanged;
        //Delegate for the QueryPiece event
        public delegate void                              QueryPieceEventHandler(object sender, QueryPieceEventArgs e);
        //Called when chess control in design mode need to know which piece to insert in the board
        public event QueryPieceEventHandler?              QueryPiece;
        //Delegate for the QueryPawnPromotionType event
        public delegate void                              QueryPawnPromotionTypeEventHandler(object sender, QueryPawnPromotionTypeEventArgs e);
        //Called when chess control needs to know which type of pawn promotion must be done
        public event QueryPawnPromotionTypeEventHandler?  QueryPawnPromotionType;
        //Called to refreshed the command state (menu, toolbar etc.)
        public event EventHandler?                        UpdateCmdState;
        public event EventHandler?                        FindMoveBegin;
        public event EventHandler?                        FindMoveEnd;

        private const string                              m_ctlIsBusyMsg = "Control is busy";
        private PieceSet?                                 m_pieceSet;
        private ChessBoard                                m_board;
        //Array of frames containing the chess piece
        private readonly Border[]                         m_borders;
        private readonly ChessBoard.PieceType[]           m_pieceType;
        //for reverse
        private bool                                      m_whiteInBottom = true;
        private int                                       m_selectedCell;
        private int                                       m_busyCount;
        private readonly System.Threading.EventWaitHandle m_actionDoneSignal;
        #endregion

        #region Board creation
        static ChessBoardControl() {
            LiteCellColorProperty   = DependencyProperty.Register("LiteCellColor",
                                                                  typeof(Color),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata(Colors.Moccasin,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                ColorInfoChanged));
            IsBoardReversedProperty  = DependencyProperty.Register("IsBoardReversed",
                                                                  typeof(bool),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata((object)false,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                IsBoardReversedChanged));
            DarkCellColorProperty   = DependencyProperty.Register("DarkCellColor",
                                                                  typeof(Color),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata(Colors.SaddleBrown,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                ColorInfoChanged));
            WhitePieceColorProperty = DependencyProperty.Register("WhitePieceColor",
                                                                  typeof(Color),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata(Colors.White,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                ColorInfoChanged));
            BlackPieceColorProperty = DependencyProperty.Register("BlackPieceColor",
                                                                  typeof(Color),
                                                                  typeof(ChessBoardControl),
                                                                  new FrameworkPropertyMetadata(Colors.Black,
                                                                                                FrameworkPropertyMetadataOptions.AffectsRender,
                                                                                                ColorInfoChanged));
            MoveFlashingProperty    = DependencyProperty.Register("MoveFlashing", 
                                                                  typeof(bool),
                                                                  typeof(ChessBoardControl), 
                                                                  new FrameworkPropertyMetadata(true));
        }
        public ChessBoardControl() {
            InitializeComponent();
            m_busyCount        = 0;
            m_actionDoneSignal = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset);
            m_selectedCell     = -1;
            m_borders          = new Border[64];
            m_pieceType        = new ChessBoard.PieceType[64];
            AutoSelection      = true;
            GameTimer          = new GameTimer {
                Enabled = false
            };
            WhitePlayerName    = "Player 1";
            BlackPlayerName    = "Player 2";
            WhitePlayerType    = PgnPlayerType.Human;
            BlackPlayerType    = PgnPlayerType.Human;
            InitCell(isReverse: false);
            IsDirty            = false;
        }

        private void RefreshBoardColor() {
            int    pos;
            Border border;
            Brush  darkBrush;
            Brush  liteBrush;

            pos       = 63;
            darkBrush = new SolidColorBrush(DarkCellColor);
            liteBrush = new SolidColorBrush(LiteCellColor);
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    border            = m_borders[pos--];
                    border.Background = (((x + y) & 1) == 0) ? liteBrush : darkBrush;
                }
            }
        }
        //Convert a board position to a ui position
        private static int UiPositionToBoardPosition(int uiPos, bool isReverse) => isReverse ? uiPos : 63 - uiPos;
        private void SetRowChr(bool isReverse) {
            UniformGrid[] grids = [m_rowChr1, m_rowChr2];

            for (int pos = 0; pos < 8; pos++) {
                foreach (UniformGrid grid in grids) {
                    if (grid.Children[pos] is Label label) {
                        label.Content = isReverse ? ((char)('a' + 7 - pos)).ToString(CultureInfo.InvariantCulture) : ((char)('a' + pos)).ToString(CultureInfo.InvariantCulture);
                    }
                }
            }
        }
        private void SetNumCol(bool isReverse) {
            UniformGrid[] grids = [m_numCol1, m_numCol2];

            for (int pos = 0; pos < 8; pos++) {
                foreach (UniformGrid grid in grids) {
                    if (grid.Children[pos] is Label label) {
                        label.Content = isReverse ? (pos + 1).ToString(CultureInfo.InvariantCulture) : (8 - pos).ToString(CultureInfo.InvariantCulture);
                    }
                }
            }
        }
        //init cell pos
        private void SetCellPos(bool isReverse) {
            int    pos;
            Border border;
            int    yPos;
            int    xPos;

            pos = 63;
            for (int y = 0; y < 8; y++) {
                yPos = isReverse ? 7 - y : y;
                for (int x = 0; x < 8; x++) {
                    xPos   = isReverse ? 7 - x : x;
                    border = m_borders[pos];
                    border.SetValue(Grid.ColumnProperty, xPos);
                    border.SetValue(Grid.RowProperty, yPos);
                    pos--;
                }
            }
        }

        // Initialize the cell
        private void InitCell(bool isReverse) {
            int    pos;
            Border border;
            Brush  brushDark;
            Brush  brushLite;

            pos       = 63;
            brushDark = new SolidColorBrush(DarkCellColor);
            brushLite = new SolidColorBrush(LiteCellColor);
            for (int y = 0; y < 8; y++) {
                for (int x = 0; x < 8; x++) {
                    border = new Border() {
                        Name            = "Cell" + (pos.ToString()),
                        BorderThickness = new Thickness(0),
                        Background      = (((x + y) & 1) == 0) ? brushLite : brushDark,
                        BorderBrush     = Background
                    };
                    m_borders[pos]   = border;
                    m_pieceType[pos] = ChessBoard.PieceType.None;
                    CellContainer.Children.Add(border);
                    pos--;
                }
            }
            SetCellPos(isReverse);
            SetNumCol(isReverse);
            SetRowChr(isReverse);
        }

        private void SetPieceControl(int boardPos, PieceSet pieceSet, ChessBoard.PieceType pieceType) {
            Border       border;
            UserControl? userControlPiece;

            border           = m_borders[boardPos];
            userControlPiece = pieceSet[pieceType];
            if (userControlPiece != null) {
                userControlPiece.Margin = (border.BorderThickness.Top == 0) ? new Thickness(3) : new Thickness(1);
            }
            m_pieceType[boardPos] = pieceType;
            border.Child          = userControlPiece;
        }

        private void RefreshCell(int boardPos, bool isFullRefresh) {
            ChessBoard.PieceType pieceType;

            if (m_board != null && m_pieceSet != null) {
                pieceType = m_board[boardPos];
                if (pieceType != m_pieceType[boardPos] || isFullRefresh) {
                    SetPieceControl(boardPos, m_pieceSet, pieceType);
                }
            }
        }
        private void RefreshCell(int boardPos) => RefreshCell(boardPos, isFullRefresh: false);

        //all board
        private void Refresh(bool isFullRefresh) {
            if (m_board != null && m_pieceSet != null) {
                for (int i = 0; i < 64; i++) {
                    RefreshCell(i, isFullRefresh);
                }
            }
        }
        public void Refresh() => Refresh(isFullRefresh: false);

        public void ResetBoard() {
            m_board.ResetBoard();
            SelectedCell = -1;
            OnBoardReset(EventArgs.Empty);
            OnUpdateCmdState(EventArgs.Empty);
            GameTimer.Reset(m_board.CurrentPlayer);
            GameTimer.Enabled = false;
            Refresh(isFullRefresh: false);
            IsDirty = false;
        }
        #endregion

        #region Properties
        private static void IsBoardReversedChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ChessBoardControl me && e.OldValue != e.NewValue) {
                me.InitCell(isReverse: (bool)e.NewValue);
                me.Refresh();
            }
        }

        private static void ColorInfoChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e) {
            if (obj is ChessBoardControl me && e.OldValue != e.NewValue) {
                me.RefreshBoardColor();
            }
        }

        // Return true if board control has been changed
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsDirty { get; set; }

        //Image displayed to the button
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Category("General")]
        [Description("true if white on the top, false if black on the top")]
        public bool IsBoardReversed {
            get => (bool?)GetValue(IsBoardReversedProperty) ?? false;
            set => SetValue(IsBoardReversedProperty, value);
        }

        // Image displayed to the button
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Brushes")]
        [Description("Lite Cell Color")]
        public Color LiteCellColor {
            get => (Color)GetValue(LiteCellColorProperty);
            set => SetValue(LiteCellColorProperty, value);
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Brushes")]
        [Description("Dark Cell Color")]
        public Color DarkCellColor {
            get => (Color)GetValue(DarkCellColorProperty);
            set => SetValue(DarkCellColorProperty, value);
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Brushes")]
        [Description("White Pieces Color")]
        public Color WhitePieceColor {
            get => (Color)GetValue(WhitePieceColorProperty);
            set => SetValue(WhitePieceColorProperty, value);
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Bindable(true)]
        [Category("Brushes")]
        [Description("Black Pieces Color")]
        public Color BlackPieceColor {
            get => (Color)GetValue(BlackPieceColorProperty);
            set => SetValue(BlackPieceColorProperty, value);
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Browsable(true)]
        [Category("General")]
        [Description("Determine if a move is flashing")]
        public bool MoveFlashing  {
            get => (bool)GetValue(MoveFlashingProperty);
            set => SetValue(MoveFlashingProperty, value);
        }

        public PieceSet? PieceSet {
            get => m_pieceSet;
            set {
                if (m_pieceSet != value) {
                    m_pieceSet = value;
                    Refresh(isFullRefresh: true);
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public ChessBoard Board {
            get => m_board;
            set {
                if (m_board != value) {
                    m_board = value;
                    Refresh(isFullRefresh: false);
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public System.Threading.EventWaitHandle SignalActionDone => m_actionDoneSignal;


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public string WhitePlayerName { get; set; }


        public string BlackPlayerName { get; set; }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public PgnPlayerType WhitePlayerType { get; set; }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public PgnPlayerType BlackPlayerType { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public ChessBoard ChessBoard => m_board;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool WhiteInBottom {
            get => m_whiteInBottom;
            set {
                if (value != m_whiteInBottom) {
                    m_whiteInBottom = value;
                    Refresh(isFullRefresh: false);
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool AutoSelection { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool BoardDesignMode {
            get => m_board.IsDesignMode;
            set {
                MessageBoxResult       result;
                ChessBoard.PlayerColor nextMoveColor;
                
                if (m_board.IsDesignMode != value) {
                    if (value) {
                        m_board.OpenDesignMode();
                        m_board.MovePosStack.Clear();
                        OnBoardReset(EventArgs.Empty);
                        GameTimer.Enabled = false;
                        OnUpdateCmdState(EventArgs.Empty);
                    } else {
                        result = MessageBox.Show("Is the next move to the white?", "SrcChess", MessageBoxButton.YesNo);
                        nextMoveColor = (result == MessageBoxResult.Yes) ? ChessBoard.PlayerColor.White : ChessBoard.PlayerColor.Black;
                        if (m_board.CloseDesignMode(nextMoveColor, (ChessBoard.BoardStateMask)0, 0 /*iEnPassant*/)) {
                            OnBoardReset(EventArgs.Empty);
                            GameTimer.Reset(m_board.CurrentPlayer);
                            GameTimer.Enabled = true;
                            IsDirty           = true;
                        }
                    }
                }
            }
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public ChessBoard.PlayerColor NextMoveColor => m_board.CurrentPlayer;

        private MoveExt[] MoveList {
            get {
                MoveExt[] moves;
                int       moveCount;
                
                moveCount   = m_board.MovePosStack.PositionInList + 1;
                moves       = new MoveExt[moveCount];
                if (moveCount != 0) {
                    m_board.MovePosStack.List.CopyTo(0, moves, 0, moveCount);
                }
                return moves;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public GameTimer GameTimer { get; private set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int SelectedCell {
            get => m_selectedCell;
            set {
                SetCellSelectionState(m_selectedCell, false);
                m_selectedCell = value;
                SetCellSelectionState(m_selectedCell, true);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsCellSelected => m_selectedCell != -1;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsBusy => m_busyCount != 0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsObservingAGame { get; set; }

        #endregion

        #region Events
        protected void OnFindMoveBegin(EventArgs e) => FindMoveBegin?.Invoke(this, e);
        protected void OnFindMoveEnd(EventArgs e) => FindMoveEnd?.Invoke(this, e);
        protected void OnUpdateCmdState(EventArgs e) => UpdateCmdState?.Invoke(this, e);
        protected void OnBoardReset(EventArgs e) => BoardReset?.Invoke(this, e);
        protected void OnRedoPosChanged(EventArgs e) => RedoPosChanged?.Invoke(this, e);
        protected void OnNewMove(NewMoveEventArgs e) => NewMove?.Invoke(this, e);
        protected virtual void OnMoveSelected(MoveSelectedEventArgs e) => MoveSelected?.Invoke(this, e);
        protected virtual void OnQueryPiece(QueryPieceEventArgs e) => QueryPiece?.Invoke(this, e);
        protected virtual void OnQueryPawnPromotionType(QueryPawnPromotionTypeEventArgs e) => QueryPawnPromotionType?.Invoke(this, e);
        #endregion

        #region Methods
        public void ShowError(string errMsg) => MessageBox.Show(Window.GetWindow(this), errMsg, "...", MessageBoxButton.OK, MessageBoxImage.Error);

        public void ShowMessage(string msg) => MessageBox.Show(Window.GetWindow(this), msg, "...", MessageBoxButton.OK, MessageBoxImage.Information);

        // Set the cell selection  appearance
        private void SetCellSelectionState(int boardPos, bool isSelected) {
            Border border;

            if (boardPos != -1) {
                border                 = m_borders[boardPos];
                border.BorderBrush     = (isSelected) ? Brushes.Black : border.Background;
                border.BorderThickness = (isSelected) ? new Thickness(1) : new Thickness(0);
                if (border.Child is Control ctl ) {
                    ctl.Margin  = (isSelected) ? new Thickness(1) : new Thickness(3);
                }
            }
        }
        private void InitAfterLoad(string whitePlayerName, string blackPlayerName, long whiteTicks, long blackTicks) {
            OnBoardReset(EventArgs.Empty);
            OnUpdateCmdState(EventArgs.Empty);
            Refresh(isFullRefresh: false);
            WhitePlayerName = whitePlayerName;
            BlackPlayerName = blackPlayerName;
            IsDirty         = false;
            GameTimer.ResetTo(m_board.CurrentPlayer, whiteTicks, blackTicks);
            GameTimer.Enabled = true;
        }

        //not used
        public virtual bool LoadGame(BinaryReader reader) {
            bool    retVal;
            string  version;
            string  whitePlayerName;
            string  blackPlayerName;
            long    whiteTicks;
            long    blackTicks;
            
            version = reader.ReadString();
            if (version != "SRCBC095") {
                retVal = false;
            } else {
                retVal = m_board.LoadBoard(reader);
                if (retVal) {
                    whitePlayerName = reader.ReadString();
                    blackPlayerName = reader.ReadString();
                    whiteTicks      = reader.ReadInt64();
                    blackTicks      = reader.ReadInt64();
                    InitAfterLoad(whitePlayerName, blackPlayerName, whiteTicks, blackTicks);
                }
            }
            return retVal;
        }

        //not used
        public bool SaveToFile() {
            return false;
        }

        //for puzzles
        public virtual void CreateGameFromMove(ChessBoard?            startingChessBoard,
                                               List<MoveExt>          moveList,
                                               ChessBoard.PlayerColor nextMoveColor,
                                               string                 whitePlayerName,
                                               string                 blackPlayerName,
                                               PgnPlayerType          whitePlayerType,
                                               PgnPlayerType          blackPlayerType,
                                               TimeSpan               whitePlayerSpan,
                                               TimeSpan               blackPlayerSpan) {
            m_board.CreateGameFromMove(startingChessBoard, moveList, nextMoveColor);
            OnBoardReset(EventArgs.Empty);
            WhitePlayerName = whitePlayerName;
            BlackPlayerName = blackPlayerName;
            WhitePlayerType = whitePlayerType;
            BlackPlayerType = blackPlayerType;
            OnUpdateCmdState(EventArgs.Empty);
            GameTimer.ResetTo(m_board.CurrentPlayer,
                              whitePlayerSpan.Ticks,
                              blackPlayerSpan.Ticks);
            GameTimer.Enabled = true;
            IsDirty           = false;
            Refresh(isFullRefresh: false);
        }        
       

        //important
        public bool GetCellFromPoint(MouseEventArgs e, out int cellPos) {
            bool   retVal;
            Point  pt;
            int    col;
            int    row;
            double actualWidth;
            double actualHeight;

            pt           = e.GetPosition(CellContainer);
            actualHeight = CellContainer.ActualHeight;
            actualWidth  = CellContainer.ActualWidth;
            col          = (int)(pt.X * 8 / actualWidth);
            row          = (int)(pt.Y * 8 / actualHeight);
            if (col >= 0 && col < 8 && row >= 0 && row < 8) {
                cellPos = (row << 3) + col;
                retVal  = true;
            } else {
                cellPos = -1;
                retVal  = false;
            }
            return retVal;
        }

        public void FlashCell(IntPoint cellPos) {
            int       absCellPos;
            Border    border;
            Brush     brush;
            object?   oriBrush;
            Color     colorStart;
            Color     colorEnd;
            SyncFlash syncFlash;
            
            m_busyCount++;  // When flashing, a message loop is processed which can cause reentrance problem
            try { 
                absCellPos = cellPos.X + cellPos.Y * 8;
                if (((cellPos.X + cellPos.Y) & 1) != 0) {
                    colorStart = DarkCellColor;
                    colorEnd   = LiteCellColor;
                } else {
                    colorStart = LiteCellColor;
                    colorEnd   = DarkCellColor;
                }
                border   = m_borders[absCellPos];
                oriBrush = border.Background.ReadLocalValue(BackgroundProperty);
                if (oriBrush == DependencyProperty.UnsetValue) {
                    oriBrush = null;
                }
                brush             = border.Background.Clone();
                border.Background = brush;
                syncFlash         = new SyncFlash(this, (SolidColorBrush)brush, colorStart, colorEnd);
                syncFlash.Flash();
                if (oriBrush == null) {
                    border.Background.ClearValue(BackgroundProperty);
                } else {
                    border.Background = (Brush)oriBrush;
                }
            } finally {
                m_busyCount--;
            }
        }

        private void FlashCell(int startPos) => FlashCell(new IntPoint(startPos & 7, startPos / 8));

        private int[] GetPosToUpdate(Move movePos) {
            List<int> retVal = new(2);

            if ((movePos.Type & Move.MoveType.MoveTypeMask) == Move.MoveType.Castle) {
                switch(movePos.EndPos) {
                case 1:
                    retVal.Add(0);
                    retVal.Add(2);
                    break;
                case 5:
                    retVal.Add(7);
                    retVal.Add(4);
                    break;
                case 57:
                    retVal.Add(56);
                    retVal.Add(58);
                    break;
                case 61:
                    retVal.Add(63);
                    retVal.Add(60);
                    break;
                default:
                    ShowError("Oops!");
                    break;
                }
            } else if ((movePos.Type & Move.MoveType.MoveTypeMask) == Move.MoveType.EnPassant) {
                retVal.Add((movePos.StartPos & 56) + (movePos.EndPos & 7));
            }
            return [..retVal];
        }

        //flash
        private void ShowBeforeMove(MoveExt movePos, bool flash) {
            if (flash) {
                FlashCell(movePos.Move.StartPos);
            }
        }
        private void ShowAfterMove(MoveExt movePos, bool flash) {
            int[] posToUpdate;

            RefreshCell(movePos.Move.StartPos);
            RefreshCell(movePos.Move.EndPos);
            if (flash) {
                FlashCell(movePos.Move.EndPos);
            }
            posToUpdate = GetPosToUpdate(movePos.Move);
            foreach (int pos in posToUpdate) {
                if (flash) {
                    FlashCell(pos);
                }
                RefreshCell(pos);
            }
        }

        public ChessBoard.GameResult DoMove(MoveExt move, bool flash) {
            ChessBoard.GameResult retVal;

            if (m_busyCount != 0) { 
                throw new MethodAccessException(m_ctlIsBusyMsg);
            }
            if (!m_board.IsMoveValid(move.Move)) {
                throw new ArgumentException("Try to make an illegal move", nameof(move));
            }
            m_actionDoneSignal.Reset();
            ShowBeforeMove(move, flash);
            retVal = m_board.DoMove(move);
            ShowAfterMove(move, flash);
            OnNewMove(new NewMoveEventArgs(move, retVal));
            OnUpdateCmdState(EventArgs.Empty);
            GameTimer.PlayerColor = m_board.CurrentPlayer;
            GameTimer.Enabled     = (retVal == ChessBoard.GameResult.OnGoing || retVal == ChessBoard.GameResult.Check);
            m_actionDoneSignal.Set();
            return retVal;
        }
        // returns: NoRepeat, FiftyRuleRepeat, ThreeFoldRepeat, Tie, Check, Mate
        public ChessBoard.GameResult DoMove(MoveExt move) => DoMove(move, MoveFlashing);

        public ChessBoard.GameResult DoUserMove(MoveExt move) {
            ChessBoard.GameResult  retVal;

            retVal  = DoMove(move);
            IsDirty = true;
            return retVal;
        }
        #endregion

    }
}
