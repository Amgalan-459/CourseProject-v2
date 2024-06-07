using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Linq;
using System.Globalization;
using SrcChess2.Core;
using SrcChess2.PgnParsing;

namespace SrcChess2 {
    public static class PgnUtil {

        [Flags]
        private enum PgnAmbiguity {
            NotFound         = 0,
            Found            = 1,
            ColMustBeSpecify = 2,
            RowMustBeSpecify = 4
        }

        public class FilterClause {
            public  bool                        IsAllRanges { get; set; }
            public  bool                        IncludesUnrated { get; set; }
            public  Dictionary<int, int>?       HashRanges { get; set; }
            public  bool                        IncludeAllPlayers { get; set; }
            public  Dictionary<string,string?>? HashPlayerList { get; set; }
            public  bool                        IncludeAllEnding { get; set; }
            public  bool                        IncludeWhiteWinningEnding { get; set; }
            public  bool                        IncludeBlackWinningEnding { get; set; }
            public  bool                        IncludeDrawEnding { get; set; }
        }
        
        public static StreamWriter? CreateOutFile(string outFileName) {
            StreamWriter? retVal;
            Stream        streamOut;
            
            try {
                streamOut = File.Create(outFileName);
                retVal    = new StreamWriter(streamOut, Encoding.GetEncoding("utf-8"));
            } catch(Exception) {
                MessageBox.Show($"Unable to create the file - {outFileName}");
                retVal = null;
            }
            return retVal;
        }

        private static void WritePgn(PgnLexical pgnBuffer, TextWriter writer, PgnGame pgnGame) => writer.Write(pgnBuffer.GetStringAtPos(pgnGame.StartingPos, pgnGame.Length));

        private static void GetPgnGameInfo(PgnGame      rawGame,
                                           out string?  gameResult,
                                           out string?  gameDate) {
            if (rawGame.Attrs == null || !rawGame.Attrs.TryGetValue("Result", out gameResult)) {
                gameResult = null;
            }
            if (rawGame.Attrs == null || !rawGame.Attrs.TryGetValue("Date", out gameDate)) {
                gameDate = null;
            }
        }

        
        private static bool IsRetained(PgnGame rawGame, int avgElo, FilterClause filterClause) {
            bool retVal;

            if (avgElo == -1) {
                retVal = filterClause.IncludesUnrated;
            } else if (filterClause.IsAllRanges) {
                retVal = true;
            } else {
                avgElo = avgElo / 100 * 100;
                retVal = filterClause.HashRanges!.ContainsKey(avgElo);
            }
            if (retVal) {
                if (!filterClause.IncludeAllPlayers || !filterClause.IncludeAllEnding) {
                    GetPgnGameInfo(rawGame, out string? gameResult,out _);
                    if (!filterClause.IncludeAllPlayers) {
                        if (!filterClause.HashPlayerList!.ContainsKey(rawGame.BlackPlayerName ?? "") &&
                            !filterClause.HashPlayerList!.ContainsKey(rawGame.WhitePlayerName ?? "")) {
                            retVal = false;
                        }
                    }
                    if (retVal && !filterClause.IncludeAllEnding) {
                        if (gameResult == "1-0") {
                            retVal = filterClause.IncludeWhiteWinningEnding;
                        } else if (gameResult == "0-1") {
                            retVal = filterClause.IncludeBlackWinningEnding;
                        } else if (gameResult == "1/2-1/2") {
                            retVal = filterClause.IncludeDrawEnding;
                        } else {
                            retVal = false;
                        }
                    }
                }                
            }
            return retVal;
        }

        public static int FilterPgn(PgnParser pgnParser, List<PgnGame> rawGames, TextWriter? textWriter, FilterClause filterClause) {
            int retVal;
            int whiteElo;
            int blackElo;
            int avgElo;
            
            retVal = 0;
            try {
                foreach (PgnGame rawGame in rawGames) {
                    whiteElo = rawGame.WhiteElo;
                    blackElo = rawGame.BlackElo;
                    avgElo   = (whiteElo != -1 && blackElo != -1) ? (whiteElo + blackElo) / 2 : -1;
                    if (IsRetained(rawGame, avgElo, filterClause)) {
                        if (textWriter != null) {
                            WritePgn(pgnParser.PgnLexical!, textWriter, rawGame);
                        }
                        retVal++;
                    }
                }
                textWriter?.Flush();
            } catch(Exception exc) {
                MessageBox.Show($"Error writing in destination file.\r\n{exc.Message}");
                retVal = 0;
            }
            return retVal;
        }

        public static int GetSquareIdFromPgn(string move) {
            int  retVal;
            char chr1;
            char chr2;
            
            if (move.Length != 2) {
                retVal = -1;
            } else {
                chr1 = move.ToLower()[0];
                chr2 = move[1];
                if (chr1 < 'a' || chr1 > 'h' || chr2 < '1' || chr2 > '8') {
                    retVal = -1;
                } else {
                    retVal = 7 - (chr1 - 'a') + ((chr2 - '0') << 3);
                }
            }            
            return retVal;
        }

        public static string GetPgnSquareId(int pos) => ((char)('a' + 7 - (pos & 7))).ToString() + ((char)((pos >> 3) + '1')).ToString();

        private static PgnAmbiguity FindMoveAmbiguity(ChessBoard chessBoard, Move move, ChessBoard.PlayerColor playerColor) {
            PgnAmbiguity         retVal = PgnAmbiguity.NotFound;
            ChessBoard.PieceType pieceType;
            List<Move>           moveList;
            
            moveList  = chessBoard.EnumMoveList(playerColor);
            pieceType = chessBoard[move.StartPos];
            foreach (Move moveTest in moveList.Where(x => x.EndPos == move.EndPos)) {
                if (moveTest.StartPos == move.StartPos) {
                    if (moveTest.Type == move.Type) {
                        retVal |= PgnAmbiguity.Found;
                    }
                } else {
                    if (chessBoard[moveTest.StartPos] == pieceType) {
                        if ((moveTest.StartPos & 7) != (move.StartPos & 7)) {
                            retVal |= PgnAmbiguity.ColMustBeSpecify;
                        } else {
                            retVal |= PgnAmbiguity.RowMustBeSpecify;
                        }
                    }
                }
            }
            return retVal;
        }

        public static string GetPgnMoveFromMove(ChessBoard chessBoard, MoveExt move, bool includeEnding) {
            string                 retVal;
            string                 startPos;
            ChessBoard.PieceType   pieceType;
            PgnAmbiguity           ambiguity;
            ChessBoard.PlayerColor playerColor;
            
            if (move.Move.Type == Move.MoveType.Castle) {
                retVal = (move.Move.EndPos == 1 || move.Move.EndPos == 57) ? "O-O" : "O-O-O";
            } else {
                pieceType   = chessBoard[move.Move.StartPos] & ChessBoard.PieceType.PieceMask;
                playerColor = chessBoard.CurrentPlayer;
                ambiguity   = FindMoveAmbiguity(chessBoard, move.Move, playerColor);
                retVal      = pieceType switch {
                    ChessBoard.PieceType.King   => "K",
                    ChessBoard.PieceType.Queen  => "Q",
                    ChessBoard.PieceType.Rook   => "R",
                    ChessBoard.PieceType.Bishop => "B",
                    ChessBoard.PieceType.Knight => "N",
                    ChessBoard.PieceType.Pawn   => "",
                    _                           => "",
                };
                startPos = GetPgnSquareId(move.Move.StartPos);
                if ((ambiguity & PgnAmbiguity.ColMustBeSpecify) == PgnAmbiguity.ColMustBeSpecify) {
                    retVal += startPos[0];
                }
                if ((ambiguity & PgnAmbiguity.RowMustBeSpecify) == PgnAmbiguity.RowMustBeSpecify) {
                    retVal += startPos[1];
                }
                if ((move.Move.Type & Move.MoveType.PieceEaten) == Move.MoveType.PieceEaten) {
                    if (pieceType == ChessBoard.PieceType.Pawn                          && 
                        (ambiguity & PgnAmbiguity.ColMustBeSpecify) == (PgnAmbiguity)0  &&
                        (ambiguity & PgnAmbiguity.RowMustBeSpecify) == (PgnAmbiguity)0) {
                        retVal += startPos[0];
                    }
                    retVal += 'x';
                }
                retVal += GetPgnSquareId(move.Move.EndPos);
                switch(move.Move.Type & Move.MoveType.MoveTypeMask) {
                case Move.MoveType.PawnPromotionToQueen:
                    retVal += "=Q";
                    break;
                case Move.MoveType.PawnPromotionToRook:
                    retVal += "=R";
                    break;
                case Move.MoveType.PawnPromotionToBishop:
                    retVal += "=B";
                    break;
                case Move.MoveType.PawnPromotionToKnight:
                    retVal += "=N";
                    break;
                default:
                    break;
                }
            }
            chessBoard.DoMoveNoLog(move.Move);
            switch(chessBoard.GetCurrentResult()) {
            case ChessBoard.GameResult.OnGoing:
                break;
            case ChessBoard.GameResult.Check:
                retVal += "+";
                break;
            case ChessBoard.GameResult.Mate:
                retVal += "#";
                if (includeEnding) {
                    if (chessBoard.CurrentPlayer == ChessBoard.PlayerColor.Black) {
                        retVal += " 1-0";
                    } else {
                        retVal += " 0-1";
                    }
                }
                break;
            case ChessBoard.GameResult.ThreeFoldRepeat:
            case ChessBoard.GameResult.FiftyRuleRepeat:
            case ChessBoard.GameResult.TieNoMove:
            case ChessBoard.GameResult.TieNoMatePossible:
                if (includeEnding) {
                    retVal += " 1/2-1/2";
                }
                break;
            default:
                break;
            }
            chessBoard.UndoMoveNoLog(move.Move);
            return retVal;
        }

        // Generates FEN
        public static string GetFenFromBoard(ChessBoard chessBoard) {
            StringBuilder             strBuilder;
            int                       emptyCount;
            ChessBoard.PieceType      pieceType;
            Char                      pieceChr;
            ChessBoard.PlayerColor    nextMoveColor;
            ChessBoard.BoardStateMask boardStateMask;
            int                       enPassantPos;
            int                       halfMoveClock;
            int                       halfMoveCount;
            int                       fullMoveCount;
            bool                      isCastling;
            
            strBuilder     = new StringBuilder(512);
            nextMoveColor  = chessBoard.CurrentPlayer;
            boardStateMask = chessBoard.ComputeBoardExtraInfo(addRepetitionInfo: false);
            enPassantPos   = (int)(boardStateMask & ChessBoard.BoardStateMask.EnPassant);
            for (int row = 7; row >= 0; row--) {
                emptyCount = 0;
                for (int col = 7; col >= 0; col--) {
                    pieceType = chessBoard[(row << 3) + col];
                    if (pieceType == ChessBoard.PieceType.None) {
                        emptyCount++;
                    } else {
                        if (emptyCount != 0) {
                            strBuilder.Append(emptyCount.ToString(CultureInfo.InvariantCulture));
                            emptyCount = 0;
                        }
                        pieceChr = (pieceType & ChessBoard.PieceType.PieceMask) switch {
                            ChessBoard.PieceType.King   => 'K',
                            ChessBoard.PieceType.Queen  => 'Q',
                            ChessBoard.PieceType.Rook   => 'R',
                            ChessBoard.PieceType.Bishop => 'B',
                            ChessBoard.PieceType.Knight => 'N',
                            ChessBoard.PieceType.Pawn   => 'P',
                            _                           => '?',
                        };
                        if ((pieceType & ChessBoard.PieceType.Black) == ChessBoard.PieceType.Black) {
                            pieceChr = Char.ToLower(pieceChr);
                        }
                        strBuilder.Append(pieceChr);
                    }
                }
                if (emptyCount != 0) {
                    strBuilder.Append(emptyCount.ToString(CultureInfo.InvariantCulture));
                }
                if (row != 0) {
                    strBuilder.Append('/');
                }
            }
            strBuilder.Append(' ');
            strBuilder.Append((nextMoveColor == ChessBoard.PlayerColor.White) ? 'w' : 'b');
            strBuilder.Append(' ');
            isCastling = false;
            if ((boardStateMask & ChessBoard.BoardStateMask.WRCastling) == ChessBoard.BoardStateMask.WRCastling) {
                strBuilder.Append('K');
                isCastling = true;
            }
            if ((boardStateMask & ChessBoard.BoardStateMask.WLCastling) == ChessBoard.BoardStateMask.WLCastling) {
                strBuilder.Append('Q');
                isCastling = true;
            }
            if ((boardStateMask & ChessBoard.BoardStateMask.BRCastling) == ChessBoard.BoardStateMask.BRCastling) {
                strBuilder.Append('k');
                isCastling = true;
            }
            if ((boardStateMask & ChessBoard.BoardStateMask.BLCastling) == ChessBoard.BoardStateMask.BLCastling) {
                strBuilder.Append('q');
                isCastling = true;
            }
            if (!isCastling) {
                strBuilder.Append('-');
            }
            strBuilder.Append(' ');
            if (enPassantPos == 0) {
                strBuilder.Append('-');
            } else {
                strBuilder.Append(GetPgnSquareId(enPassantPos));
            }
            halfMoveClock  = chessBoard.MoveHistory.GetCurrent50RulePlyCount;
            halfMoveCount  = chessBoard.MovePosStack.PositionInList + 1;
            fullMoveCount  = (halfMoveCount + 2) / 2;
            strBuilder.Append($" {halfMoveClock.ToString(CultureInfo.InvariantCulture)} {fullMoveCount.ToString(CultureInfo.InvariantCulture)}");
            return strBuilder.ToString();
        }

        public static string GetPgnFromBoard(ChessBoard    chessBoard,
                                             bool          includeRedoMove,
                                             string        eventName,
                                             string        site,
                                             string        date,
                                             string        round,
                                             string        whitePlayerName,
                                             string        blackPlayerName,
                                             PgnPlayerType whitePlayerType,
                                             PgnPlayerType blackPlayerType,
                                             TimeSpan      whitePlayerTime,
                                             TimeSpan      blackPlayerTime) {
            int           moveIndex;
            StringBuilder strBuilder;
            StringBuilder lineStrBuilder;
            int           oriIndex;
            int           moveCount;
            MovePosStack  movePosStack;
            MoveExt       move;
            movePosStack   = chessBoard.MovePosStack;
            oriIndex       = movePosStack.PositionInList;
            moveCount      = (includeRedoMove) ? movePosStack.Count : oriIndex + 1;
            strBuilder     = new StringBuilder(10 * moveCount + 256);
            lineStrBuilder = new StringBuilder(256);
            string result  = chessBoard.GetCurrentResult() switch {
                ChessBoard.GameResult.Check or ChessBoard.GameResult.OnGoing
                    => "*",
                ChessBoard.GameResult.Mate
                    => (chessBoard.CurrentPlayer == ChessBoard.PlayerColor.White) ? "0-1" : "1-0",
                ChessBoard.GameResult.FiftyRuleRepeat or ChessBoard.GameResult.ThreeFoldRepeat or ChessBoard.GameResult.TieNoMove or ChessBoard.GameResult.TieNoMatePossible
                    => "1/2-1/2",
                _ 
                    => "*",
            };
            chessBoard.UndoAllMoves();
            strBuilder.Append($"[Event \"{eventName}\"]\n");
            strBuilder.Append($"[Site \"{site}\"]\n");
            strBuilder.Append($"[Date \"{date}\"]\n");
            strBuilder.Append($"[Round \"{round}\"]\n");
            strBuilder.Append($"[White \"{whitePlayerName}\"]\n");
            strBuilder.Append($"[Black \"{blackPlayerName}\"]\n");
            strBuilder.Append($"[Result \"{result}\"]\n");
            if (!chessBoard.IsStdInitialBoard) {
                strBuilder.Append("[SetUp \"1\"]\n");
                strBuilder.Append($"[FEN \"{GetFenFromBoard(chessBoard)}\"]\n");
            }
            strBuilder.Append($"[WhiteType \"{((whitePlayerType == PgnPlayerType.Human) ? "human" : "program")}\"]\n");
            strBuilder.Append($"[BlackType \"{((blackPlayerType == PgnPlayerType.Human) ? "human" : "program")}\"]\n");
            strBuilder.Append($"[TimeControl \"?:{whitePlayerTime.Ticks.ToString(CultureInfo.InvariantCulture)}:{blackPlayerTime.Ticks.ToString(CultureInfo.InvariantCulture)}\"]\n");
            strBuilder.Append('\n');
            lineStrBuilder.Length = 0;
            for (moveIndex = 0; moveIndex < moveCount; moveIndex++) {
                if (lineStrBuilder.Length > 60) {
                    strBuilder.Append(lineStrBuilder);
                    strBuilder.Append('\n');
                    lineStrBuilder.Length = 0;
                }
                move = movePosStack[moveIndex];
                if ((moveIndex & 1) == 0) {
                    lineStrBuilder.Append(((moveIndex + 1) / 2 + 1).ToString(CultureInfo.InvariantCulture));
                    lineStrBuilder.Append(". ");
                }
                lineStrBuilder.Append(GetPgnMoveFromMove(chessBoard, move, true) + " ");
                chessBoard.RedoMove();
            }
            lineStrBuilder.Append(' ');
            lineStrBuilder.Append(result);
            strBuilder.Append(lineStrBuilder);
            strBuilder.Append('\n');
            return strBuilder.ToString();
        }

        public static string[] GetPgnArrayFromMoveList(ChessBoard chessBoard) {
            string[]     retVal;
            int          oriPos;
            int          moveIndex;
            MovePosStack moveStack;
            
            oriPos    = chessBoard.MovePosStack.PositionInList;
            chessBoard.UndoAllMoves();
            moveStack = chessBoard.MovePosStack;
            retVal    = new string[moveStack.Count];
            moveIndex = 0;
            foreach (MoveExt move in moveStack.List) {
                retVal[moveIndex++] = GetPgnMoveFromMove(chessBoard, move, includeEnding: false);
                chessBoard.RedoMove();
            }
            chessBoard.SetUndoRedoPosition(oriPos);
            return retVal;
        }
    }
}
