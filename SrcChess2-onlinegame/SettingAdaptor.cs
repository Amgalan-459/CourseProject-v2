using System;
using System.Collections.Generic;
using System.Windows.Media;
using SrcChess2.FicsInterface;
using System.Windows;
using SrcChess2.Core;

namespace SrcChess2 {

    internal class SettingAdaptor {

        public SettingAdaptor(Properties.Settings settings) => Settings = settings;

        public Properties.Settings Settings { get; private set; }

        private static Color NameToColor(string colorName) {
            Color   retVal;

            if (colorName.Length == 8 && (Char.IsLower(colorName[0]) || Char.IsDigit(colorName[0])) &&
                int.TryParse(colorName, System.Globalization.NumberStyles.HexNumber, null, out int val)) {
                retVal = Color.FromArgb((byte)((val >> 24) & 255), (byte)((val >> 16) & 255), (byte)((val >> 8) & 255), (byte)(val & 255));
            } else {
                retVal = (Color)ColorConverter.ConvertFromString(colorName);
            }
            return (retVal);    
        }

        public void LoadFicsConnectionSetting(FicsConnectionSetting ficsSetting) {
            ficsSetting.HostName  = Settings.FICSHostName;
            ficsSetting.HostPort  = Settings.FICSHostPort;
            ficsSetting.UserName  = Settings.FICSUserName;
            ficsSetting.Anonymous = string.Compare(Settings.FICSUserName, "guest", true) == 0;
        }

        public void SaveFicsConnectionSetting(FicsConnectionSetting ficsSetting) {
            Settings.FICSHostName = ficsSetting.HostName;
            Settings.FICSHostPort = ficsSetting.HostPort;
            Settings.FICSUserName = ficsSetting.Anonymous ? "Guest" : ficsSetting.UserName;
        }
        public void LoadChessBoardCtl(ChessBoardControl chessCtl) {
            chessCtl.LiteCellColor   = NameToColor(Settings.LiteCellColor);
            chessCtl.DarkCellColor   = NameToColor(Settings.DarkCellColor);
            chessCtl.WhitePieceColor = NameToColor(Settings.WhitePieceColor);
            chessCtl.BlackPieceColor = NameToColor(Settings.BlackPieceColor);
            chessCtl.MoveFlashing    = Settings.FlashPiece;
        }

        public void SaveChessBoardCtl(ChessBoardControl chessCtl) {
            Settings.WhitePieceColor = chessCtl.WhitePieceColor.ToString();
            Settings.BlackPieceColor = chessCtl.BlackPieceColor.ToString();
            Settings.LiteCellColor   = chessCtl.LiteCellColor.ToString();
            Settings.DarkCellColor   = chessCtl.DarkCellColor.ToString();
            Settings.FlashPiece      = chessCtl.MoveFlashing;
        }

        public void LoadMainWindow(MainWindow mainWnd, SortedList<string,PieceSet> pieceSetList) {
            mainWnd.m_colorBackground = NameToColor(Settings.BackgroundColor);
            mainWnd.Background        = new SolidColorBrush(mainWnd.m_colorBackground);
            mainWnd.PieceSet          = pieceSetList[Settings.PieceSet];
            if (!Enum.TryParse(Settings.WndState, out WindowState windowState)) {
                windowState = WindowState.Normal;
            }
            mainWnd.WindowState = windowState;
            mainWnd.Height      = Settings.WndHeight;
            mainWnd.Width       = Settings.WndWidth;
            if (!double.IsNaN(Settings.WndLeft)) {
                mainWnd.Left = Settings.WndLeft;
            }
            if (!double.IsNaN(Settings.WndTop)) {
                mainWnd.Top = Settings.WndTop;
            }
            mainWnd.m_puzzleMasks[0] = Settings.PuzzleDoneLow;
            mainWnd.m_puzzleMasks[1] = Settings.PuzzleDoneHigh;
        }

        public void SaveMainWindow(MainWindow mainWnd) {
            Settings.BackgroundColor = mainWnd.m_colorBackground.ToString();
            Settings.PieceSet        = mainWnd.PieceSet?.Name ?? "???";
            Settings.WndState        = mainWnd.WindowState.ToString();
            Settings.WndHeight       = mainWnd.Height;
            Settings.WndWidth        = mainWnd.Width;
            Settings.WndLeft         = mainWnd.Left;
            Settings.WndTop          = mainWnd.Top;
            Settings.PuzzleDoneLow   = mainWnd.m_puzzleMasks[0];
            Settings.PuzzleDoneHigh  = mainWnd.m_puzzleMasks[1];
        }

        public void LoadFICSSearchCriteria(SearchCriteria searchCriteria) {
            searchCriteria.PlayerName        = Settings.FICSSPlayerName;
            searchCriteria.BlitzGame         = Settings.FICSSBlitz;
            searchCriteria.LightningGame     = Settings.FICSSLightning;
            searchCriteria.UntimedGame       = Settings.FICSSUntimed;
            searchCriteria.StandardGame      = Settings.FICSSStandard;
            searchCriteria.IsRated           = Settings.FICSSRated;
            searchCriteria.MinRating         = SearchCriteria.CnvToNullableIntValue(Settings.FICSSMinRating);
            searchCriteria.MinTimePerPlayer  = SearchCriteria.CnvToNullableIntValue(Settings.FICSSMinTimePerPlayer);
            searchCriteria.MaxTimePerPlayer  = SearchCriteria.CnvToNullableIntValue(Settings.FICSSMaxTimePerPlayer);
            searchCriteria.MinIncTimePerMove = SearchCriteria.CnvToNullableIntValue(Settings.FICSSMinIncTimePerMove);
            searchCriteria.MaxIncTimePerMove = SearchCriteria.CnvToNullableIntValue(Settings.FICSSMaxIncTimePerMove);
            searchCriteria.MaxMoveDone       = Settings.FICSSMaxMoveDone;
            searchCriteria.MoveTimeOut       = SearchCriteria.CnvToNullableIntValue(Settings.FICSMoveTimeOut);
        }
    }
}
