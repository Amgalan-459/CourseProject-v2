using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SrcChess2.Core;
using SrcChess2.PgnParsing;

namespace SrcChess2 {
    public partial class FrmLoadPuzzle : Window {
        public class PuzzleItem(int id, string description, bool isDone) {
            public int    Id { get; private set; } = id;
            public string Description { get; private set; } = description;
            public bool   Done { get; set; } = isDone;
        }

        static private List<PgnGame>? m_pgnGameList;
        private readonly PgnParser    m_pgnParser;
        private readonly long[]?      m_doneMask;
        public FrmLoadPuzzle(long[]? doneMask) {
            List<PuzzleItem> puzzleItemList;
            PuzzleItem       puzzleItem;
            int              count;
            bool             hasBeenDone;

            InitializeComponent();
            m_doneMask  = doneMask;
            m_pgnParser = new PgnParser(false);
            if (m_pgnGameList == null) {
                BuildPuzzleList();
            }
            puzzleItemList = new List<PuzzleItem>(m_pgnGameList!.Count);
            count          = 0;
            foreach (PgnGame pgnGame in m_pgnGameList) {
                if (doneMask == null) {
                    hasBeenDone = false;
                } else {
                    hasBeenDone = (doneMask[count / 64] & (1L << (count & 63))) != 0;
                }
                count++;
                puzzleItem  = new(count, pgnGame.Event ?? "", hasBeenDone);
                puzzleItemList.Add(puzzleItem);
            }
            listViewPuzzle.ItemsSource   = puzzleItemList;
            listViewPuzzle.SelectedIndex = 0;
        }

        public FrmLoadPuzzle() : this(null) {}

        private string LoadPgn() {
            string                 retVal;
            Assembly               assem;
            System.IO.Stream?      stream;
            System.IO.StreamReader reader;

            assem   = GetType().Assembly;
            stream  = assem.GetManifestResourceStream("SrcChess2.111probs.pgn");
            reader  = new System.IO.StreamReader(stream ?? throw new InvalidOperationException("Unable to find the SrcChess2.111probs.pgn resource"),
                                                 Encoding.ASCII);
            try {
                retVal = reader.ReadToEnd();
            } finally {
                reader.Dispose();
            }
            return retVal;
        }

        private void BuildPuzzleList() {
            string  pgn;

            pgn           = LoadPgn();
            m_pgnParser.InitFromString(pgn);
            m_pgnGameList = m_pgnParser.GetAllRawPgn(getAttrList: true, getMoveList: false, out int _);
        }

        public PgnGame Game {
            get {
                PgnGame retVal;

                retVal                    = m_pgnGameList![listViewPuzzle.SelectedIndex];
                m_pgnParser.ParseFen(retVal.Fen ?? "", out ChessBoard.PlayerColor playerColor, out ChessBoard? board);
                retVal.StartingColor      = playerColor;
                retVal.StartingChessBoard = board;
                return retVal;
            }
        }

        public int GameIndex => listViewPuzzle.SelectedIndex;

        private void ButOk_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        private void ButCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void ButResetDone_Click(object sender, RoutedEventArgs e) {
            List<PuzzleItem> puzzleItemList;

            if (MessageBox.Show("Are you sure you want to reset the Done state of all puzzles to false?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes) {
                for (int i = 0; i < m_doneMask!.Length; i++) {
                    m_doneMask[i] = 0;
                }
                puzzleItemList = (List<PuzzleItem>)listViewPuzzle.ItemsSource;
                foreach (PuzzleItem item in puzzleItemList) {
                    item.Done = false;
                }
                listViewPuzzle.ItemsSource = null;
                listViewPuzzle.ItemsSource = puzzleItemList;
            }
        }

        private void ListViewPuzzle_SelectionChanged(object sender, SelectionChangedEventArgs e) => butOk.IsEnabled = listViewPuzzle.SelectedIndex != -1;

        private void ListViewPuzzle_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (listViewPuzzle.SelectedIndex != -1) {
                DialogResult = true;
            }
        }
    }
}
