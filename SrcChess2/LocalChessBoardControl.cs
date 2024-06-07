using System;
using System.Collections.Generic;
using System.IO;
using SrcChess2.Core;
using SrcChess2.PgnParsing;

namespace SrcChess2 {
    internal class LocalChessBoardControl : ChessBoardControl {
        public  MainWindow? ParentBoardWindow { get; set; }
        public LocalChessBoardControl() : base() {}

        //not used
        public override bool LoadGame(BinaryReader reader) {
            bool                       retVal;
            string                     version;
            MainWindow.MainPlayingMode playingMode;
                
            version = reader.ReadString();
            if (version == "SRCCHESS095") {
                retVal = base.LoadGame(reader);
                if (retVal) {
                    playingMode                    = (MainWindow.MainPlayingMode)reader.ReadInt32();
                    ParentBoardWindow!.PlayingMode = playingMode;
                } else {
                    retVal = false;
                }
            } else {
                retVal = false;
            }
            return retVal;
        }

        //for puzzles
        public override void CreateGameFromMove(ChessBoard?            startingChessBoard,
                                                List<MoveExt>          moveList,
                                                ChessBoard.PlayerColor nextMoveColor,
                                                string                 whitePlayerName,
                                                string                 blackPlayerName,
                                                PgnPlayerType          whitePlayerType,
                                                PgnPlayerType          blackPlayerType,
                                                TimeSpan               whitePlayerTime,
                                                TimeSpan               blackPlayerTime) {
            base.CreateGameFromMove(startingChessBoard,
                                    moveList,
                                    nextMoveColor,
                                    whitePlayerName,
                                    blackPlayerName,
                                    whitePlayerType,
                                    blackPlayerType,
                                    whitePlayerTime,
                                    blackPlayerTime);
            if (whitePlayerType == PgnPlayerType.Program) {
                if (blackPlayerType == PgnPlayerType.Program) {
                    ParentBoardWindow!.PlayingMode = MainWindow.MainPlayingMode.ComputerPlayBoth;
                } else {
                    ParentBoardWindow!.PlayingMode = MainWindow.MainPlayingMode.ComputerPlayWhite;
                }
            } else if (blackPlayerType == PgnPlayerType.Program) {
                ParentBoardWindow!.PlayingMode = MainWindow.MainPlayingMode.ComputerPlayBlack;
            } else {
                ParentBoardWindow!.PlayingMode = MainWindow.MainPlayingMode.PlayerAgainstPlayer;
            }
            MainWindow.SetCmdState();
        }
    }
}
