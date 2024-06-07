using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SrcChess2 {
    /// <summary>Pickup the colors use to draw the chess control</summary>
    public partial class FrmBoardSetting : Window {
        public Color                                 LiteCellColor { get; private set; }
        public Color                                 DarkCellColor { get; private set; }
        public Color                                 WhitePieceColor { get; private set; }
        public Color                                 BlackPieceColor { get; private set; }
        public Color                                 BackgroundColor { get; private set; }
        public PieceSet?                             PieceSet { get; private set; }
        private readonly SortedList<string,PieceSet> m_pieceSetList = new(0);

        public FrmBoardSetting() => InitializeComponent();

        public FrmBoardSetting(Color liteCellColor, Color darkCellColor, Color whitePieceColor, Color blackPieceColor, Color backGroundColor, SortedList<string, PieceSet> pieceSetList, PieceSet pieceSet) {
            InitializeComponent();
            LiteCellColor              = liteCellColor;
            DarkCellColor              = darkCellColor;
            WhitePieceColor            = whitePieceColor;
            BlackPieceColor            = blackPieceColor;
            BackgroundColor            = backGroundColor;
            m_pieceSetList             = pieceSetList;
            PieceSet                   = pieceSet;
            m_chessCtl.LiteCellColor   = liteCellColor;
            m_chessCtl.DarkCellColor   = darkCellColor;
            m_chessCtl.WhitePieceColor = whitePieceColor;
            m_chessCtl.BlackPieceColor = blackPieceColor;
            m_chessCtl.PieceSet        = pieceSet;
            Background                 = new SolidColorBrush(BackgroundColor);
            Loaded                    += new RoutedEventHandler(FrmBoardSetting_Loaded);
            FillPieceSet();
        }

        private void FrmBoardSetting_Loaded(object sender, RoutedEventArgs e) {
            customColorPickerLite.SelectedColor         = LiteCellColor;
            customColorPickerDark.SelectedColor         = DarkCellColor;
            customColorBackground.SelectedColor         = BackgroundColor;
            customColorPickerLite.SelectedColorChanged += new Action<Color>(CustomColorPickerLite_SelectedColorChanged);
            customColorPickerDark.SelectedColorChanged += new Action<Color>(CustomColorPickerDark_SelectedColorChanged);
            customColorBackground.SelectedColorChanged += new Action<Color>(CustomColorBackground_SelectedColorChanged);
        }

        private void CustomColorPickerDark_SelectedColorChanged(Color color) {
            DarkCellColor            = color;
            m_chessCtl.DarkCellColor = DarkCellColor;
        }

        private void CustomColorPickerLite_SelectedColorChanged(Color color) {
            LiteCellColor            = color;
            m_chessCtl.LiteCellColor = LiteCellColor;
        }

        private void CustomColorBackground_SelectedColorChanged(Color color) {
            BackgroundColor = color;
            Background      = new SolidColorBrush(BackgroundColor);
        }

        private void FillPieceSet() {
            int index;

            comboBoxPieceSet.Items.Clear();
            foreach (PieceSet pieceSet in m_pieceSetList.Values) {
                index = comboBoxPieceSet.Items.Add(pieceSet.Name);
                if (pieceSet == PieceSet) {
                    comboBoxPieceSet.SelectedIndex = index;
                }
            }
        }

        private void ButResetToDefault_Click(object sender, RoutedEventArgs e) {
            LiteCellColor                       = Colors.Moccasin;
            DarkCellColor                       = Colors.SaddleBrown;
            BackgroundColor                     = Colors.SkyBlue;
            PieceSet                            = m_pieceSetList["leipzig"];
            Background                          = new SolidColorBrush(BackgroundColor);
            m_chessCtl.LiteCellColor            = LiteCellColor;
            m_chessCtl.DarkCellColor            = DarkCellColor;
            m_chessCtl.PieceSet                 = PieceSet;
            customColorPickerLite.SelectedColor = LiteCellColor;
            customColorPickerDark.SelectedColor = DarkCellColor;
            customColorBackground.SelectedColor = BackgroundColor;
            comboBoxPieceSet.SelectedItem       = PieceSet.Name;
        }

        private void ComboBoxPieceSet_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            int     selectedIndex;
            string  val;

            selectedIndex  = comboBoxPieceSet.SelectedIndex;
            if (selectedIndex != -1) {
                val                 = (string)comboBoxPieceSet.Items[selectedIndex];
                PieceSet            = m_pieceSetList[val];
                m_chessCtl.PieceSet = PieceSet;
            }
        }

        private void ButOk_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }
    }
}
