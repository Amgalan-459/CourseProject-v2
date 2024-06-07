using System;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using SrcChess2.Core;

namespace SrcChess2 {
    public class MoveItem {
        public  string  Step { get; set; }
        public  string  Who { get; set; }
        public  string  Move { get; set; }

        public MoveItem(string step, string who, string move) {
            Step = step;
            Who  = who;
            Move = move; 
        }
    }

    public class MoveItemList : ObservableCollection<MoveItem> {}

    public partial class MoveViewer : UserControl {

        public enum ViewerDisplayMode {
            MovePos,
            Pgn
        }
        
        public class NewMoveSelectedEventArg : System.ComponentModel.CancelEventArgs {
            public int NewIndex { get; set; }
            public NewMoveSelectedEventArg(int newIndex) : base(false) => NewIndex = newIndex;

        }
        
        public event EventHandler<NewMoveSelectedEventArg>? NewMoveSelected;
        private ChessBoardControl?                          m_chessCtl;
        private ViewerDisplayMode                           m_displayMode;
        private bool                                        m_ignoreChg;
        public  MoveItemList                                MoveList { get; }

        public MoveViewer() {
            InitializeComponent();
            m_displayMode                      = ViewerDisplayMode.MovePos;
            m_ignoreChg                        = false;
            MoveList                           = (MoveItemList)listViewMoveList.ItemsSource;
            listViewMoveList.SelectionChanged += new SelectionChangedEventHandler(ListViewMoveList_SelectionChanged);
        }

        public ChessBoardControl? ChessControl {
            get => m_chessCtl;
            set {
                if (m_chessCtl != value) {
                    if (m_chessCtl != null) { 
                        m_chessCtl.BoardReset     -= ChessCtl_BoardReset;
                        m_chessCtl.NewMove        -= ChessCtl_NewMove;
                        m_chessCtl.RedoPosChanged -= ChessCtl_RedoPosChanged;
                    }
                    m_chessCtl = value;
                    if (m_chessCtl != null) { 
                        m_chessCtl.BoardReset     += ChessCtl_BoardReset;
                        m_chessCtl.NewMove        += ChessCtl_NewMove;
                        m_chessCtl.RedoPosChanged += ChessCtl_RedoPosChanged;
                    }
                }
            }
        }

        private string GetMoveDesc(MoveExt move) {
            string  retVal;
            
            if (m_displayMode == ViewerDisplayMode.MovePos) {
                retVal = move.GetHumanPos();
            } else {
                retVal = PgnUtil.GetPgnMoveFromMove(m_chessCtl!.Board, move, includeEnding: false);
                if ((move.Move.Type & Move.MoveType.MoveFromBook) == Move.MoveType.MoveFromBook) {
                    retVal = $"({retVal})";
                }
            }
            return retVal;
        }

        private void Redisplay() {
            string[]?    moveNames;
            int          moveCount;
            MovePosStack movePosStack;
            MoveExt      move;
            string       moveTxt;
            string       moveIndex;
            MoveItem     moveItem;
            ChessBoard   chessBoard;

            chessBoard = m_chessCtl!.Board;
            if (chessBoard != null) {
                movePosStack = chessBoard.MovePosStack;
                moveCount    = movePosStack.Count;
                if (moveCount != 0) {
                    if (m_displayMode == ViewerDisplayMode.MovePos) {
                        moveNames = null;
                    } else {
                        moveNames = PgnUtil.GetPgnArrayFromMoveList(chessBoard);
                    }
                    for (int i = 0; i < moveCount; i++) {
                        move = movePosStack[i];
                        if (m_displayMode == ViewerDisplayMode.MovePos) {
                            moveTxt   = move.GetHumanPos();
                            moveIndex = (i + 1).ToString();
                        } else {
                            moveTxt   = moveNames![i];
                            moveIndex = (i / 2 + 1).ToString() + ((Char)('a' + (i & 1))).ToString();
                        }
                        moveItem      = MoveList![i];
                        MoveList[i]   = new MoveItem(moveIndex, moveItem.Who, moveTxt);
                    }
                }
            }
        }

        private void AddCurrentMove() {
            MoveItem               moveItem;
            string                 moveTxt;
            string                 moveIndex;
            int                    moveCount;
            int                    itemCount;
            int                    index;
            MoveExt                move;
            ChessBoard.PlayerColor playerToMove;
            ChessBoard             chessBoard;

            chessBoard   = m_chessCtl!.Board;            
            m_ignoreChg  = true;
            move         = chessBoard.MovePosStack.CurrentMove;
            playerToMove = chessBoard.LastMovePlayer;
            chessBoard.UndoMove();
            moveCount    = chessBoard.MovePosStack.Count;
            itemCount    = listViewMoveList.Items.Count;
            while (itemCount >= moveCount) {
                itemCount--;
                MoveList.RemoveAt(itemCount);
            }
            moveTxt = GetMoveDesc(move);
            chessBoard.RedoMove();
            index     = itemCount;
            moveIndex = (m_displayMode == ViewerDisplayMode.MovePos) ? (index + 1).ToString() : (index / 2 + 1).ToString() + ((Char)('a' + (index & 1))).ToString();
            moveItem  = new MoveItem(moveIndex,
                                     (playerToMove == ChessBoard.PlayerColor.Black) ? "Black" : "White",
                                     moveTxt);
            MoveList.Add(moveItem);
            m_ignoreChg = false;
        }

        private void SelectCurrentMove() {
            int        index;
            MoveItem   moveItem;
            ChessBoard chessBoard;

            chessBoard  = m_chessCtl!.Board;
            m_ignoreChg = true;
            index       = chessBoard.MovePosStack.PositionInList;
            if (index == -1) {
                listViewMoveList.SelectedItem = null;
            } else {
                moveItem                      = (MoveItem)listViewMoveList.Items[index];
                listViewMoveList.SelectedItem = moveItem;
                listViewMoveList.ScrollIntoView(moveItem);
            }
            m_ignoreChg = false;
        }

        public ViewerDisplayMode DisplayMode {
            get => m_displayMode;
            set {
                if (value != m_displayMode) {
                    m_displayMode = value;
                    Redisplay();
                }
            }
        }

        private void Reset() {
            int         count;
            ChessBoard  chessBoard;
            
            MoveList.Clear();
            chessBoard = m_chessCtl!.Board;
            count      = chessBoard.MovePosStack.Count;
            chessBoard.UndoAllMoves();
            for (int i = 0; i < count; i++) {
                chessBoard.RedoMove();
                AddCurrentMove();
            }
            SelectCurrentMove();
        }

        protected void OnNewMoveSelected(NewMoveSelectedEventArg e) => NewMoveSelected?.Invoke(this, e);


        private void ChessCtl_RedoPosChanged(object? sender, EventArgs e) => SelectCurrentMove();

        private void ChessCtl_NewMove(object? sender, ChessBoardControl.NewMoveEventArgs e) {
            AddCurrentMove();
            SelectCurrentMove();
        }

        private void ChessCtl_BoardReset(object? sender, EventArgs e) => Reset();

        private void ListViewMoveList_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
            NewMoveSelectedEventArg evArg;
            int                     curPos;
            int                     newPos;
            ChessBoard              chessBoard;
            
            if (!m_ignoreChg && !m_chessCtl!.IsBusy && !ChessBoardControl.IsSearchEngineBusy) {
                m_ignoreChg = true;
                chessBoard  = m_chessCtl!.Board;
                curPos      = chessBoard.MovePosStack.PositionInList;
                if (e.AddedItems.Count != 0) {
                    newPos = listViewMoveList.SelectedIndex;
                    if (newPos != curPos) {
                        evArg = new NewMoveSelectedEventArg(newPos);
                        OnNewMoveSelected(evArg);
                        if (evArg.Cancel) {
                            if (curPos == -1) {
                                listViewMoveList.SelectedItems.Clear();
                            } else {
                                listViewMoveList.SelectedIndex  = curPos;
                            }
                        }
                    }
                }
                m_ignoreChg = false;
            }
        }
    }
}
