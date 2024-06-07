using System.Windows;
using SrcChess2.Core;

namespace SrcChess2 {
    public partial class FrmGameParameter : Window {
        private readonly MainWindow          m_parentWindow = null!;
        private readonly BoardEvaluationUtil m_boardEvalUtil = null!;
        private readonly ChessSearchSetting  m_chessSearchSetting = null!;
        public FrmGameParameter() => InitializeComponent();

        private FrmGameParameter(MainWindow parent, ChessSearchSetting chessSearchSetting, BoardEvaluationUtil boardEvalUtil) : this() {
            m_parentWindow       = parent;
            m_chessSearchSetting = chessSearchSetting;
            m_boardEvalUtil      = boardEvalUtil;
            switch(m_parentWindow.PlayingMode) {
            case MainWindow.MainPlayingMode.DesignMode:
                throw new System.ApplicationException("Must not be called in design mode.");
            case MainWindow.MainPlayingMode.ComputerPlayWhite:
            case MainWindow.MainPlayingMode.ComputerPlayBlack:
                radioButtonPlayerAgainstComputer.IsChecked = true;
                radioButtonPlayerAgainstComputer.Focus();
                break;
            case MainWindow.MainPlayingMode.PlayerAgainstPlayer:
                radioButtonPlayerAgainstPlayer.IsChecked = true;
                radioButtonPlayerAgainstPlayer.Focus();
                break;
            case MainWindow.MainPlayingMode.ComputerPlayBoth:
                radioButtonComputerAgainstComputer.IsChecked = true;
                radioButtonComputerAgainstComputer.Focus();
                break;
            }
            if (m_parentWindow.PlayingMode == MainWindow.MainPlayingMode.ComputerPlayBlack) { 
                radioButtonComputerPlayBlack.IsChecked = true;
            } else {
                radioButtonComputerPlayWhite.IsChecked = true;
            }
            switch (m_chessSearchSetting.DifficultyLevel) {
            case ChessSearchSetting.SettingDifficultyLevel.Manual:
                radioButtonLevelManual.IsChecked = true;
                break;
            case ChessSearchSetting.SettingDifficultyLevel.VeryEasy:
                radioButtonLevel1.IsChecked = true;
                break;
            case ChessSearchSetting.SettingDifficultyLevel.Easy:
                radioButtonLevel2.IsChecked = true;
                break;
            case ChessSearchSetting.SettingDifficultyLevel.Intermediate:
                radioButtonLevel3.IsChecked = true;
                break;
            case ChessSearchSetting.SettingDifficultyLevel.Hard:
                radioButtonLevel4.IsChecked = true;
                break;
            case ChessSearchSetting.SettingDifficultyLevel.VeryHard:
                radioButtonLevel5.IsChecked = true;
                break;
            default:
                radioButtonLevel1.IsChecked = true;
                break;
            }
            CheckState();
            radioButtonLevel1.ToolTip      = chessSearchSetting.HumanSearchMode(ChessSearchSetting.SettingDifficultyLevel.VeryEasy);
            radioButtonLevel2.ToolTip      = chessSearchSetting.HumanSearchMode(ChessSearchSetting.SettingDifficultyLevel.Easy);
            radioButtonLevel3.ToolTip      = chessSearchSetting.HumanSearchMode(ChessSearchSetting.SettingDifficultyLevel.Intermediate);
            radioButtonLevel4.ToolTip      = chessSearchSetting.HumanSearchMode(ChessSearchSetting.SettingDifficultyLevel.Hard);
            radioButtonLevel5.ToolTip      = chessSearchSetting.HumanSearchMode(ChessSearchSetting.SettingDifficultyLevel.VeryHard);
            radioButtonLevelManual.ToolTip = chessSearchSetting.HumanSearchMode(ChessSearchSetting.SettingDifficultyLevel.Manual);
        }
        private void CheckState() {
            groupBoxComputerPlay.IsEnabled = radioButtonPlayerAgainstComputer.IsChecked!.Value;
            butUpdManual.IsEnabled         = radioButtonLevelManual.IsChecked == true;
        }

        private void ButOk_Click(object sender, RoutedEventArgs e) {
            if (radioButtonPlayerAgainstComputer.IsChecked == true) {
                m_parentWindow!.PlayingMode = (radioButtonComputerPlayBlack.IsChecked == true) ? MainWindow.MainPlayingMode.ComputerPlayBlack : MainWindow.MainPlayingMode.ComputerPlayWhite;
            } else if (radioButtonPlayerAgainstPlayer.IsChecked == true) {
                m_parentWindow!.PlayingMode = MainWindow.MainPlayingMode.PlayerAgainstPlayer;
            } else if (radioButtonComputerAgainstComputer.IsChecked == true) {
                m_parentWindow!.PlayingMode = MainWindow.MainPlayingMode.ComputerPlayBoth;
            }
            DialogResult = true;
            Close();
        }
        private void ButUpdManual_Click(object sender, RoutedEventArgs e) {
            FrmSearchMode frm;

            frm = new(m_chessSearchSetting, m_boardEvalUtil) {
                Owner = this
            };
            if (frm.ShowDialog() == true) {
                m_parentWindow!.SetSearchMode(m_chessSearchSetting.GetBoardSearchSetting());
            }
        }

        private void RadioButtonOpponent_CheckedChanged(object sender, RoutedEventArgs e) => CheckState();
        private void RadioButtonLevelManual_CheckedChanged(object sender, RoutedEventArgs e) => CheckState();

        public static bool AskGameParameter(MainWindow parent, ChessSearchSetting chessSearchSetting, BoardEvaluationUtil boardEvalUtil) {
            bool             retVal;
            FrmGameParameter frm;

            frm = new(parent, chessSearchSetting, boardEvalUtil) {
                Owner = parent
            };
            retVal     = (frm.ShowDialog() == true);
            if (retVal) {                
                if (frm.radioButtonLevel1.IsChecked == true) {
                    frm.m_chessSearchSetting.DifficultyLevel = ChessSearchSetting.SettingDifficultyLevel.VeryEasy;
                } else if (frm.radioButtonLevel2.IsChecked == true) {
                    frm.m_chessSearchSetting.DifficultyLevel = ChessSearchSetting.SettingDifficultyLevel.Easy;
                } else if (frm.radioButtonLevel3.IsChecked == true) {
                    frm.m_chessSearchSetting.DifficultyLevel = ChessSearchSetting.SettingDifficultyLevel.Intermediate;
                } else if (frm.radioButtonLevel4.IsChecked == true) {
                    frm.m_chessSearchSetting.DifficultyLevel = ChessSearchSetting.SettingDifficultyLevel.Hard;
                } else if (frm.radioButtonLevel5.IsChecked == true) {
                    frm.m_chessSearchSetting.DifficultyLevel = ChessSearchSetting.SettingDifficultyLevel.VeryHard;
                } else if (frm.radioButtonLevelManual.IsChecked == true) {
                    frm.m_chessSearchSetting.DifficultyLevel = ChessSearchSetting.SettingDifficultyLevel.Manual;
                }
                frm.m_parentWindow!.SetSearchMode(frm.m_chessSearchSetting.GetBoardSearchSetting());
            }
            return retVal;
        }

    }
}
