using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using GenericSearchEngine;
using SrcChess2.Core;

namespace SrcChess2 {
    public partial class FrmSearchMode : Window {
        private readonly ChessSearchSetting   m_chessSearchSetting = null!;
        private readonly BoardEvaluationUtil? m_boardEvalUtil;

        public FrmSearchMode() => InitializeComponent();
        public FrmSearchMode(ChessSearchSetting chessSearchSetting, BoardEvaluationUtil boardEvalUtil) : this() {
            int pos;
            
            m_chessSearchSetting = chessSearchSetting;
            m_boardEvalUtil      = boardEvalUtil;
            foreach (IBoardEvaluation boardEval in m_boardEvalUtil.BoardEvaluators) {
                pos = comboBoxWhiteBEval.Items.Add(boardEval.Name);
                if (chessSearchSetting.WhiteBoardEvaluator == boardEval) {
                    comboBoxWhiteBEval.SelectedIndex = pos;
                }
                pos = comboBoxBlackBEval.Items.Add(boardEval.Name);
                if (chessSearchSetting.BlackBoardEvaluator == boardEval) {
                    comboBoxBlackBEval.SelectedIndex = pos;
                }
            }
            if (chessSearchSetting.ThreadingMode == ThreadingMode.OnePerProcessorForSearch) {
                radioButtonOnePerProc.IsChecked = true;
            } else if (chessSearchSetting.ThreadingMode == ThreadingMode.DifferentThreadForSearch) {
                radioButtonOneForUI.IsChecked = true;
            } else {
                radioButtonNoThread.IsChecked = true;
            }
            if (chessSearchSetting.BookMode == ChessSearchSetting.BookModeSetting.NoBook) {
                radioButtonNoBook.IsChecked = true;
            } else if (chessSearchSetting.BookMode == ChessSearchSetting.BookModeSetting.Unrated) {
                radioButtonUnrated.IsChecked = true;
            } else {
                radioButtonELO2500.IsChecked = true;
            }
            if ((chessSearchSetting.SearchOption & SearchOption.UseAlphaBeta) != 0) {
                radioButtonAlphaBeta.IsChecked = true;
            } else {
                radioButtonMinMax.IsChecked  = true;
            }
            if (chessSearchSetting.SearchDepth == 0) {
                radioButtonAvgTime.IsChecked = true;
                textBoxTimeInSec.Text        = chessSearchSetting.TimeOutInSec.ToString(CultureInfo.InvariantCulture);
                plyCount.Value               = 6;
            } else {
                if ((chessSearchSetting.SearchOption & SearchOption.UseIterativeDepthSearch) == SearchOption.UseIterativeDepthSearch) {
                    radioButtonFixDepthIterative.IsChecked = true;
                } else {
                    radioButtonFixDepth.IsChecked = true;
                }
                plyCount.Value        = chessSearchSetting.SearchDepth;
                textBoxTimeInSec.Text = "15";
            }
            plyCount2.Content   = plyCount.Value.ToString();
            switch(chessSearchSetting.RandomMode) {
            case RandomMode.Off:
                radioButtonRndOff.IsChecked = true;
                break;
            case RandomMode.OnRepetitive:
                radioButtonRndOnRep.IsChecked = true;
                break;
            default:
                radioButtonRndOn.IsChecked = true;
                break;
            }
            textBoxTransSize.Text  = (chessSearchSetting.TransTableEntryCount / 1000000 * 32).ToString(CultureInfo.InvariantCulture);    // Roughly 32 bytes / entry
            checkBoxTransTable.IsChecked = (chessSearchSetting.SearchOption & SearchOption.UseTransTable) != 0;
            plyCount.ValueChanged += new RoutedPropertyChangedEventHandler<double>(PlyCount_ValueChanged);
        }

        private void PlyCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => plyCount2.Content = plyCount.Value.ToString(CultureInfo.InvariantCulture);

        private void SetPlyAvgTimeState() {
            if (radioButtonAvgTime.IsChecked == true) {
                plyCount.IsEnabled         = false;
                labelNumberOfPly.IsEnabled = false;
                textBoxTimeInSec.IsEnabled = true;
                labelAvgTime.IsEnabled     = true;
            } else {
                plyCount.IsEnabled         = true;
                labelNumberOfPly.IsEnabled = true;
                textBoxTimeInSec.IsEnabled = false;
                labelAvgTime.IsEnabled     = false;
            }
        }

        private void RadioButtonSearchType_CheckedChanged(object sender, RoutedEventArgs e) => SetPlyAvgTimeState();

        private void TextBoxTimeInSec_TextChanged(object sender, TextChangedEventArgs e)
            => butOk.IsEnabled = (int.TryParse(textBoxTimeInSec.Text, out int val) && val > 0 && val < 999);

        private void TextBoxTransSize_TextChanged(object sender, TextChangedEventArgs e)
            => butOk.IsEnabled = (int.TryParse(textBoxTransSize.Text, out int val) && val > 4 && val < 1000);

        private void UpdateSearchMode() {
            int               transTableSize;
            IBoardEvaluation? boardEval;

            m_chessSearchSetting.SearchOption = (radioButtonAlphaBeta.IsChecked == true) ? SearchOption.UseAlphaBeta : SearchOption.UseMinMax;
            if (radioButtonNoBook.IsChecked == true) {
                m_chessSearchSetting.BookMode = ChessSearchSetting.BookModeSetting.NoBook;
            } else if (radioButtonUnrated.IsChecked == true) {
                m_chessSearchSetting.BookMode = ChessSearchSetting.BookModeSetting.Unrated;
            } else {
                m_chessSearchSetting.BookMode = ChessSearchSetting.BookModeSetting.ELOGT2500;
            }
            if (checkBoxTransTable.IsChecked == true) {
                m_chessSearchSetting.SearchOption |= SearchOption.UseTransTable;
            }
            if (radioButtonOnePerProc.IsChecked == true) {
                m_chessSearchSetting.ThreadingMode = ThreadingMode.OnePerProcessorForSearch;
            } else if (radioButtonOneForUI.IsChecked == true) {
                m_chessSearchSetting.ThreadingMode = ThreadingMode.DifferentThreadForSearch;
            } else {
                m_chessSearchSetting.ThreadingMode = ThreadingMode.Off;
            }
            if (radioButtonAvgTime.IsChecked == true) {
                m_chessSearchSetting.SearchDepth  = 0;
                m_chessSearchSetting.TimeOutInSec = int.Parse(textBoxTimeInSec.Text);
            } else {
                m_chessSearchSetting.SearchDepth  = (int)plyCount.Value;
                m_chessSearchSetting.TimeOutInSec = 0;
                if (radioButtonFixDepthIterative.IsChecked == true) {
                    m_chessSearchSetting.SearchOption |= SearchOption.UseIterativeDepthSearch;
                }
            }
            if (radioButtonRndOff.IsChecked == true) {
                m_chessSearchSetting.RandomMode = RandomMode.Off;
            } else if (radioButtonRndOnRep.IsChecked == true) {
                m_chessSearchSetting.RandomMode = RandomMode.OnRepetitive;
            } else {
                m_chessSearchSetting.RandomMode = RandomMode.On;
            }
            transTableSize                            = int.Parse(textBoxTransSize.Text);
            m_chessSearchSetting.TransTableEntryCount = transTableSize / 32 * 1000000;
            boardEval                                 = m_boardEvalUtil!.FindBoardEvaluator(comboBoxWhiteBEval.SelectedItem.ToString());
            boardEval                               ??= m_boardEvalUtil.BoardEvaluators[0];
            m_chessSearchSetting.WhiteBoardEvaluator  = boardEval;
            boardEval                                 = m_boardEvalUtil.FindBoardEvaluator(comboBoxBlackBEval.SelectedItem.ToString());
            boardEval                               ??= m_boardEvalUtil.BoardEvaluators[0];
            m_chessSearchSetting.BlackBoardEvaluator  = boardEval;
        }
        private void ButOk_Click(object sender, RoutedEventArgs e) {
            UpdateSearchMode();
            DialogResult = true;
            Close();
        }
    }
}
