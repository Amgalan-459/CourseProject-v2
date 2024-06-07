using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using GenericSearchEngine;

namespace SrcChess2.Core {
    /// <summary>
    /// Implementation of the chess board without any user interface.
    /// </summary>
    public sealed class ChessBoard : ISearchTrace<Move> {
        public enum PlayerColor {
            White   = 0,
            Black   = 1
        }
        
        public enum SerPieceType : byte {
            Empty       = 0,
            WhitePawn   = 1,
            WhiteKnight = 2,
            WhiteBishop = 3,
            WhiteRook   = 4,
            WhiteQueen  = 5,
            WhiteKing   = 6,
            NotUsed1    = 7,
            NotUsed2    = 8,
            BlackPawn   = 9,
            BlackKnight = 10,
            BlackBishop = 11,
            BlackRook   = 12,
            BlackQueen  = 13,
            BlackKing   = 14,
            NotUsed3    = 15,
        }
        
        /// <summary>Value of each piece on the board. Each piece is a combination of piece value and color (0 for white, 8 for black)</summary>
        [Flags]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "<Pending>")]
        public enum PieceType : byte {
            None      = 0,
            Pawn      = 1,
            Knight    = 2,
            Bishop    = 3,
            Rook      = 4,
            Queen     = 5,
            King      = 6,
            PieceMask = 7,
            Black     = 8,
            White     = 0,
        }
        
        [Flags]
        public enum ValidPawnPromotion {
            None   = 0,
            Queen  = 1,
            Rook   = 2,
            Bishop = 4,
            Knight = 8
        };

        //Mask for board extra info
        [Flags]
        public enum BoardStateMask {
            //0-63 to express the EnPassant possible position
            EnPassant    = 63,
            BlackToMove  = 64,
            // <summary>white left castling is possible
            WLCastling   = 128,
            //white right castling is possible
            WRCastling   = 256,
            //black left castling is possible
            BLCastling   = 512,
            //black right castling is possible
            BRCastling   = 1024,
            //Mask use to save the number of times the board has been repeated
            BoardRepMask = 2048+4096+8192
        };

        public enum RepeatResult {
            NoRepeat,
            ThreeFoldRepeat,
            FiftyRuleRepeat
        };
        
        public enum GameResult {
            OnGoing,
            ThreeFoldRepeat,
            FiftyRuleRepeat,
            TieNoMove,
            TieNoMatePossible,
            Check,
            Mate
        }

        /// <summary>NULL position info</summary>
        public static readonly AttackPosInfo s_attackPosInfoNull = new(0, 0);
        /// <summary>Possible diagonal or linear moves for each board position</summary>
        private static readonly int[][][]    s_caseMoveDiagLine;
        /// <summary>Possible diagonal moves for each board position</summary>
        private static readonly int[][][]    s_caseMoveDiagonal;
        /// <summary>Possible linear moves for each board position</summary>
        private static readonly int[][][]    s_caseMoveLine;
        /// <summary>Possible knight moves for each board position</summary>
        private static readonly int[][]      s_caseMoveKnight;
        /// <summary>Possible king moves for each board position</summary>
        private static readonly int[][]      s_caseMoveKing;
        /// <summary>Possible board positions a black pawn can attack from each board position</summary>
        private static readonly int[][]      s_caseBlackPawnCanAttackFrom;
        /// <summary>Possible board positions a white pawn can attack from each board position</summary>
        private static readonly int[][]      s_caseWhitePawnCanAttackFrom;

        /// <summary>Chess board</summary>
        ///     A  B  C  D  E  F  G  H
        ///   ---------------------------
        /// 8 | 63 62 61 60 59 58 57 56 | 8
        /// 7 | 55 54 53 52 51 50 49 48 | 7
        /// 6 | 47 46 45 44 43 42 41 40 | 6
        /// 5 | 39 38 37 36 35 34 33 32 | 5
        /// 4 | 31 30 29 28 27 26 25 24 | 4
        /// 3 | 23 22 21 20 19 18 17 16 | 3
        /// 2 | 15 14 13 12 11 10 9  8  | 2
        /// 1 | 7  6  5  4  3  2  1  0  | 1
        ///   ---------------------------
        ///     A  B  C  D  E  F  G  H
        private readonly PieceType[]           m_board;
        /// <summary>Board adaptor for the search engine</summary>
        private readonly ChessGameBoardAdaptor m_boardAdaptor;
        /// <summary>Position of the black king</summary>
        internal int                           m_blackKingPos;
        /// <summary>Position of the white king</summary>
        private int                            m_whiteKingPos;
        /// <summary>Number of pieces of each kind/color</summary>
        private readonly int[]                 m_pieceTypeCount;
        /// <summary>Random number generator</summary>
        private Random                         m_rnd;
        /// <summary>Random number generator (repetitive, seed = 0)</summary>
        private Random                         m_repRnd;
        /// <summary>Number of time the right black rook has been moved. Used to determine if castle is possible</summary>
        private int                            m_rightBlackRookMoveCount;
        /// <summary>Number of time the left black rook has been moved. Used to determine if castle is possible</summary>
        private int                            m_leftBlackRookMoveCount;
        /// <summary>Number of time the black king has been moved. Used to determine if castle is possible</summary>
        private int                            m_blackKingMoveCount;
        /// <summary>Number of time the right white rook has been moved. Used to determine if castle is possible</summary>
        private int                            m_rightWhiteRookMoveCount;
        /// <summary>Number of time the left white rook has been moved. Used to determine if castle is possible</summary>
        private int                            m_leftWhiteRookMoveCount;
        /// <summary>Number of time the white king has been moved. Used to determine if castle is possible</summary>
        private int                            m_whiteKingMoveCount;
        /// <summary>White has castle if true</summary>
        private bool                           m_isWhiteCastled;
        /// <summary>Black has castle if true</summary>
        private bool                           m_isBlackCastled;
        /// <summary>Position behind the pawn which had just been moved from 2 positions</summary>
        private int                            m_possibleEnPassantPos;
        /// <summary>Stack of m_iPossibleEnPassantAt values</summary>
        private Stack<int>                     m_pPossibleEnPassantPosStack;
        /// <summary>Information about pieces attack</summary>
        private AttackPosInfo                  m_posInfo;
        /// <summary>Opening book to use if any</summary>
        private Book?                          m_book;
        /// <summary>Object where to redirect the trace if any</summary>
        private ISearchTrace<Move>?            m_trace;

        /// Class static constructor. 
        /// Builds the list of possible moves for each piece type per position.
        /// Etablished the value of each type of piece for board evaluation.
        static ChessBoard() {
            s_attackPosInfoNull.PiecesAttacked  = 0;
            s_attackPosInfoNull.PiecesDefending = 0;
            s_caseMoveDiagLine                  = new int[64][][];
            s_caseMoveDiagonal                  = new int[64][][];
            s_caseMoveLine                      = new int[64][][];
            s_caseMoveKnight                    = new int[64][];
            s_caseMoveKing                      = new int[64][];
            s_caseWhitePawnCanAttackFrom        = new int[64][];
            s_caseBlackPawnCanAttackFrom        = new int[64][];
            for (int i = 0; i < 64; i++) {
                s_caseMoveDiagLine[i]           = GetAccessibleSquares(i,
                                                                       deltas: [-1, -1,  -1, 0,  -1, 1,  0, -1,  0, 1,  1, -1,  1, 0,  1, 1],
                                                                       canBeRepeat: true);
                s_caseMoveDiagonal[i]           = GetAccessibleSquares(i,
                                                                       deltas: [-1, -1, -1, 1, 1, -1, 1, 1],
                                                                       canBeRepeat: true);
                s_caseMoveLine[i]               = GetAccessibleSquares(i,
                                                                       deltas: [-1, 0, 1, 0, 0, -1, 0, 1],
                                                                       canBeRepeat: true);
                s_caseMoveKnight[i]             = GetAccessibleSquares(i,
                                                                       deltas: [1, 2, 1, -2, 2, -1, 2, 1, -1, 2, -1, -2, -2, -1, -2, 1],
                                                                       canBeRepeat: false)[0];
                s_caseMoveKing[i]               = GetAccessibleSquares(i,
                                                                       deltas: [-1, -1, -1, 0, -1, 1, 0, -1, 0, 1, 1, -1, 1, 0, 1, 1],
                                                                       canBeRepeat: false)[0];
                s_caseWhitePawnCanAttackFrom[i] = GetAccessibleSquares(i, 
                                                                       deltas: [-1, -1, 1, -1],
                                                                       canBeRepeat: false)[0];
                s_caseBlackPawnCanAttackFrom[i] = GetAccessibleSquares(i,
                                                                       deltas: [-1, 1, 1, 1],
                                                                       canBeRepeat: false)[0];
            }
        }

        // Get all squares which can be access by a piece positioned at squarePos
        static private int[][] GetAccessibleSquares(int squarePos, int[] deltas, bool canBeRepeat) {
            List<int[]> retVal = new(4);
            int         colPos;
            int         rowPos;
            int         colIndex;
            int         rowIndex;
            int         colDelta;
            int         rowDelta;
            int         posOfs;
            int         newPos;
            List<int>   lineSquares;

            retVal.Clear();
            lineSquares = new List<int>(8);
            colPos      = squarePos &  7;
            rowPos      = squarePos >> 3;
            for (int i = 0; i < deltas.Length; i += 2) {
                colDelta = deltas[i];
                rowDelta = deltas[i+1];
                posOfs   = (rowDelta << 3) + colDelta;
                colIndex = colPos + colDelta;
                rowIndex = rowPos + rowDelta;
                newPos   = squarePos + posOfs;
                if (canBeRepeat) {
                    lineSquares.Clear();
                    while (colIndex >= 0 && colIndex < 8 && rowIndex >= 0 && rowIndex < 8) {
                        lineSquares.Add(newPos);
                        colIndex += colDelta;
                        rowIndex += rowDelta;
                        newPos   += posOfs;
                    }
                    if (lineSquares.Count != 0) {
                        retVal.Add([.. lineSquares]);
                    }
                } else if (colIndex >= 0 && colIndex < 8 && rowIndex >= 0 && rowIndex < 8) {
                    lineSquares.Add(newPos);
                }
            }
            if (!canBeRepeat) {
                retVal.Add([.. lineSquares]);
            }
            return [.. retVal];
        }

        public ChessBoard(ISearchTrace<Move>? trace, Dispatcher dispatcher) {
            m_board                      = new PieceType[64];
            m_pieceTypeCount             = new int[16];
            m_book                       = null;
            m_rnd                        = new Random((int)DateTime.Now.Ticks);
            m_repRnd                     = new Random(0);
            m_pPossibleEnPassantPosStack = new Stack<int>(256);
            m_trace                      = trace;
            MoveHistory                  = new MoveHistory();
            IsDesignMode                 = false;
            MovePosStack                 = new MovePosStack();
            m_boardAdaptor               = new ChessGameBoardAdaptor(this, dispatcher);
            ResetBoard();
        }

        public ChessBoard(Dispatcher dispatcher) : this(trace: null!, dispatcher) {}

        //create a clone
        private ChessBoard(ChessBoard chessBoard) : this(chessBoard.m_boardAdaptor.Dispatcher) => CopyFrom(chessBoard);
        internal ChessBoard() : this((Dispatcher)null!) {}

        public void CopyFrom(ChessBoard chessBoard) {
            int[]   arr;

            chessBoard.m_board.CopyTo(m_board, 0);
            chessBoard.m_pieceTypeCount.CopyTo(m_pieceTypeCount, 0);
            arr = [.. chessBoard.m_pPossibleEnPassantPosStack];
            Array.Reverse(arr);
            m_pPossibleEnPassantPosStack = new(arr);
            m_book                       = chessBoard.m_book;
            m_blackKingPos               = chessBoard.m_blackKingPos;
            m_whiteKingPos               = chessBoard.m_whiteKingPos;
            m_rnd                        = chessBoard.m_rnd;
            m_repRnd                     = chessBoard.m_repRnd;
            m_rightBlackRookMoveCount    = chessBoard.m_rightBlackRookMoveCount;
            m_leftBlackRookMoveCount     = chessBoard.m_leftBlackRookMoveCount;
            m_blackKingMoveCount         = chessBoard.m_blackKingMoveCount;
            m_rightWhiteRookMoveCount    = chessBoard.m_rightWhiteRookMoveCount;
            m_leftWhiteRookMoveCount     = chessBoard.m_leftWhiteRookMoveCount;
            m_whiteKingMoveCount         = chessBoard.m_whiteKingMoveCount;
            m_isWhiteCastled             = chessBoard.m_isWhiteCastled;
            m_isBlackCastled             = chessBoard.m_isBlackCastled;
            m_possibleEnPassantPos       = chessBoard.m_possibleEnPassantPos;
            ZobristKey                   = chessBoard.ZobristKey;
            m_trace                      = chessBoard.m_trace;
            MovePosStack                 = chessBoard.MovePosStack.Clone();
            MoveHistory                  = chessBoard.MoveHistory.Clone();
            CurrentPlayer                = chessBoard.CurrentPlayer;
        }


        public ChessBoard Clone() => new(this);

        // Search trace
        public void LogSearchTrace(int depth, int playerId, Move move, int pts) => m_trace?.LogSearchTrace(depth, playerId, move, pts);

        /// <summary>
        /// Stack of all moves done since initial board
        /// </summary>
        public MovePosStack MovePosStack {get; private set; }

        /// <summary>
        /// Get the move history which handle the fifty-move rule and the threefold repetition rule
        /// </summary>
        public MoveHistory MoveHistory { get; private set; }

        public BoardStateMask ComputeBoardExtraInfo(bool addRepetitionInfo) {
            BoardStateMask retVal;
            
            retVal = (BoardStateMask)m_possibleEnPassantPos;
            if (m_whiteKingMoveCount == 0) {
                if (m_rightWhiteRookMoveCount == 0) {
                    retVal |= BoardStateMask.WRCastling;
                }
                if (m_leftWhiteRookMoveCount == 0) {
                    retVal |= BoardStateMask.WLCastling;
                }
            }
            if (m_blackKingMoveCount == 0) {
                if (m_rightBlackRookMoveCount == 0) {
                    retVal |= BoardStateMask.BRCastling;
                }
                if (m_leftBlackRookMoveCount == 0) {
                    retVal |= BoardStateMask.BLCastling;
                }
            }
            if (addRepetitionInfo) {
                retVal = (BoardStateMask)((MoveHistory.GetCurrentSameBoardCount(ZobristKey) & 7) << 11);
            }
            return retVal;
        }
        private void ResetInitialBoardInfo(PlayerColor nextMoveColor, bool isStdBoard, BoardStateMask boardMask, int enPassantPos) {
            PieceType pieceType;
            int       enPassantCol;

            Array.Clear(m_pieceTypeCount, 0, m_pieceTypeCount.Length);
            for (int i = 0; i < 64; i++) {
                pieceType = m_board[i];
                switch(pieceType) {
                case PieceType.King | PieceType.White:
                    m_whiteKingPos = i;
                    break;
                case PieceType.King | PieceType.Black:
                    m_blackKingPos = i;
                    break;
                }
                m_pieceTypeCount[(int)pieceType]++;
            }
            if (enPassantPos != 0) {
                enPassantCol = (enPassantPos >> 3);
                if (enPassantCol != 2 && enPassantCol != 5) {
                    if (enPassantCol == 3) {   // Fixing old saved board which was keeping the en passant position at the position of the pawn instead of behind it
                        enPassantPos -= 8;    
                    } else if (enPassantCol == 4) {
                        enPassantPos += 8;
                    } else {
                        enPassantPos = 0;
                    }
                }
            }
            m_possibleEnPassantPos    = enPassantPos;
            m_rightBlackRookMoveCount = ((boardMask & BoardStateMask.BRCastling) == BoardStateMask.BRCastling) ? 0 : 1;
            m_leftBlackRookMoveCount  = ((boardMask & BoardStateMask.BLCastling) == BoardStateMask.BLCastling) ? 0 : 1;
            m_blackKingMoveCount      = 0;
            m_rightWhiteRookMoveCount = ((boardMask & BoardStateMask.WRCastling) == BoardStateMask.WRCastling) ? 0 : 1;
            m_leftWhiteRookMoveCount  = ((boardMask & BoardStateMask.WLCastling) == BoardStateMask.WLCastling) ? 0 : 1;
            m_whiteKingMoveCount      = 0;
            m_isWhiteCastled          = false;
            m_isBlackCastled          = false;
            ZobristKey                = Core.ZobristKey.ComputeBoardZobristKey(m_board);
            CurrentPlayer             = nextMoveColor;
            IsDesignMode              = false;
            IsStdInitialBoard         = isStdBoard;
            MoveHistory.Reset(m_board, ComputeBoardExtraInfo(addRepetitionInfo: false));
            MovePosStack.Clear();
            m_pPossibleEnPassantPosStack.Clear();
        }

        public void ResetBoard() {
            for (int i = 0; i < 64; i++) {
                m_board[i] = PieceType.None;
            }
            for (int i = 0; i < 8; i++) {
                m_board[8+i]  = PieceType.Pawn | PieceType.White;
                m_board[48+i] = PieceType.Pawn | PieceType.Black;
            }
            m_board[0]     = PieceType.Rook   | PieceType.White;
            m_board[7*8]   = PieceType.Rook   | PieceType.Black;
            m_board[7]     = PieceType.Rook   | PieceType.White;
            m_board[7*8+7] = PieceType.Rook   | PieceType.Black;
            m_board[1]     = PieceType.Knight | PieceType.White;
            m_board[7*8+1] = PieceType.Knight | PieceType.Black;
            m_board[6]     = PieceType.Knight | PieceType.White;
            m_board[7*8+6] = PieceType.Knight | PieceType.Black;
            m_board[2]     = PieceType.Bishop | PieceType.White;
            m_board[7*8+2] = PieceType.Bishop | PieceType.Black;
            m_board[5]     = PieceType.Bishop | PieceType.White;
            m_board[7*8+5] = PieceType.Bishop | PieceType.Black;
            m_board[3]     = PieceType.King   | PieceType.White;
            m_board[7*8+3] = PieceType.King   | PieceType.Black;
            m_board[4]     = PieceType.Queen  | PieceType.White;
            m_board[7*8+4] = PieceType.Queen  | PieceType.Black;
            ResetInitialBoardInfo(PlayerColor.White,
                                  isStdBoard: true,
                                  BoardStateMask.BLCastling | BoardStateMask.BRCastling | BoardStateMask.WLCastling | BoardStateMask.WRCastling,
                                  enPassantPos: 0);
        }

       

        //not used
        public bool LoadBoard(BinaryReader reader) {
            bool                    retVal;
            MoveHistory.PackedBoard packedBoard;
            string                  version;
            int                     enPassantPos;
            
            version = reader.ReadString();
            if (version != "SRCBD095") {
                retVal = false;
            } else {
                retVal = true;
                ResetBoard();
                IsStdInitialBoard = reader.ReadBoolean();
                if (!IsStdInitialBoard) {
                    packedBoard.m_val1 = reader.ReadInt64();
                    packedBoard.m_val2 = reader.ReadInt64();
                    packedBoard.m_val3 = reader.ReadInt64();
                    packedBoard.m_val4 = reader.ReadInt64();
                    packedBoard.m_info = (BoardStateMask)reader.ReadInt32();
                    enPassantPos       = reader.ReadInt32();
                    MoveHistory.UnpackBoard(packedBoard, m_board);
                    CurrentPlayer      = ((packedBoard.m_info & BoardStateMask.BlackToMove) == BoardStateMask.BlackToMove) ? PlayerColor.Black : PlayerColor.White;
                    ResetInitialBoardInfo(CurrentPlayer, IsStdInitialBoard, packedBoard.m_info, enPassantPos);
                }
                MovePosStack.LoadFromReader(reader);
                for (int i = 0; i <= MovePosStack.PositionInList; i++) {
                    DoMoveNoLog(MovePosStack.List[i].Move);
                }
            }
            return retVal;
        }

        public void CreateGameFromMove(ChessBoard? chessBoardStarting, List<MoveExt> moveList, PlayerColor startingColor) {
            BoardStateMask boardMask;
            
            if (chessBoardStarting != null) {
                CopyFrom(chessBoardStarting);
                boardMask = chessBoardStarting.ComputeBoardExtraInfo(addRepetitionInfo: false);
                ResetInitialBoardInfo(startingColor, isStdBoard: false , boardMask, chessBoardStarting.m_possibleEnPassantPos);
            } else {
                ResetBoard();
            }
            foreach (MoveExt move in moveList) {
                DoMove(move);
            }
        }

        public bool IsDesignMode { get; private set; }

        public void OpenDesignMode() => IsDesignMode = true;

        public bool CloseDesignMode(PlayerColor nextMoveColor, BoardStateMask boardMask, int enPassantPos) {
            bool retVal;
            
            if (!IsDesignMode) {
                retVal = true;
            } else {
                ResetInitialBoardInfo(nextMoveColor, false, boardMask, enPassantPos);
                if (m_pieceTypeCount[(int)(PieceType.King | PieceType.White)] == 1 &&
                    m_pieceTypeCount[(int)(PieceType.King | PieceType.Black)] == 1) {
                    retVal = true;
                } else {
                    retVal = false;
                }
            }
            return retVal;
        }

        public bool IsStdInitialBoard { get; private set; }

        private void UpdatePackedBoardAndZobristKey(int chgPos, PieceType newPiece) {
            ZobristKey = Core.ZobristKey.UpdateZobristKey(ZobristKey, chgPos, m_board[chgPos], newPiece);
            MoveHistory.UpdateCurrentPackedBoard(chgPos, newPiece);
        }

        public long ZobristKey { get; private set; }

        /// <summary>
        /// Update the packed board representation and the value of the hash key representing the current board state. Use if two
        /// board positions are changed.
        /// </summary>
        private void UpdatePackedBoardAndZobristKey(int pos1, PieceType newPiece1, int pos2, PieceType newPiece2) {
            ZobristKey = Core.ZobristKey.UpdateZobristKey(ZobristKey, pos1, m_board[pos1], newPiece1, pos2, m_board[pos2], newPiece2);
            MoveHistory.UpdateCurrentPackedBoard(pos1, newPiece1);
            MoveHistory.UpdateCurrentPackedBoard(pos2, newPiece2);
        }

        public PlayerColor CurrentPlayer { get; private set; }
        public PlayerColor LastMovePlayer => CurrentPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;

        // Get a piece at the specified position. Position 0 = Lower right (H1), 63 = Higher left (A8)
        public PieceType this[int pos] {
            get => m_board[pos];
            set {
                if (IsDesignMode) {
                    if (m_board[pos] != value) {
                        m_pieceTypeCount[(int)m_board[pos]]--;
                        m_board[pos] = value;
                        m_pieceTypeCount[(int)m_board[pos]]++;
                    }
                } else {
                    throw new NotSupportedException("Cannot be used if not in design mode");
                }
            }
        }

        public int GetEatedPieceCount(PieceType pieceType) {
            var retVal = (pieceType & PieceType.PieceMask) switch {
                PieceType.Pawn                                         => 8 - m_pieceTypeCount[(int)pieceType],
                PieceType.Rook or PieceType.Knight or PieceType.Bishop => 2 - m_pieceTypeCount[(int)pieceType],
                PieceType.Queen or PieceType.King                      => 1 - m_pieceTypeCount[(int)pieceType],
                _                                                      => 0,
            };
            if (retVal < 0) {
                retVal = 0;
            }
            return retVal;
        }

        

        /// <summary>
        /// Do the move (without log)
        /// </summary>
        /// <returns>
        /// NoRepeat        No repetition
        /// ThreeFoldRepeat Three times the same board
        /// FiftyRuleRepeat Fifty moves without pawn move or piece eaten
        /// </returns>
        public RepeatResult DoMoveNoLog(Move move) {
            RepeatResult retVal;
            PieceType    pieceType;
            PieceType    oldPieceType;
            int          enPassantVictimPos;
            int          delta;
            bool         isPawnMoveOrPieceEaten;
            
            m_pPossibleEnPassantPosStack.Push(m_possibleEnPassantPos);
            m_possibleEnPassantPos = 0;
            pieceType              = m_board[move.StartPos];
            isPawnMoveOrPieceEaten = ((pieceType & PieceType.PieceMask) == PieceType.Pawn) |
                                     ((move.Type & Move.MoveType.PieceEaten) == Move.MoveType.PieceEaten);
            switch(move.Type & Move.MoveType.MoveTypeMask) {
            case Move.MoveType.Castle:
                UpdatePackedBoardAndZobristKey(move.EndPos, pieceType, move.StartPos, PieceType.None);
                m_board[move.EndPos]   = pieceType;
                m_board[move.StartPos] = PieceType.None;
                if ((pieceType & PieceType.Black) != 0) {
                    if (move.EndPos == 57) {
                        UpdatePackedBoardAndZobristKey(58, m_board[56], 56, PieceType.None);
                        m_board[58] = m_board[56];
                        m_board[56] = PieceType.None;
                    } else {
                        UpdatePackedBoardAndZobristKey(60, m_board[63], 63, PieceType.None);
                        m_board[60] = m_board[63];
                        m_board[63] = PieceType.None;
                    }
                    m_isBlackCastled = true;
                    m_blackKingPos   = move.EndPos;
                } else {
                    if (move.EndPos == 1) {
                        UpdatePackedBoardAndZobristKey(2, m_board[0], 0, PieceType.None);
                        m_board[2] = m_board[0];
                        m_board[0] = PieceType.None;
                    } else {
                        UpdatePackedBoardAndZobristKey(4, m_board[7], 7, PieceType.None);
                        m_board[4] = m_board[7];
                        m_board[7] = PieceType.None;
                    }
                    m_isWhiteCastled = true;
                    m_whiteKingPos   = move.EndPos;
                }
                break;
            case Move.MoveType.EnPassant:
                UpdatePackedBoardAndZobristKey(move.EndPos, pieceType, move.StartPos, PieceType.None);
                m_board[move.EndPos]   = pieceType;
                m_board[move.StartPos] = PieceType.None;
                enPassantVictimPos     = (move.StartPos & 56) + (move.EndPos & 7);
                oldPieceType           = m_board[enPassantVictimPos];
                UpdatePackedBoardAndZobristKey(enPassantVictimPos, PieceType.None);
                m_board[enPassantVictimPos] = PieceType.None;
                m_pieceTypeCount[(int)oldPieceType]--;
                break;
            default:
                // PawnPromotion To or normal moves
                oldPieceType = m_board[move.EndPos];
                switch(move.Type & Move.MoveType.MoveTypeMask) {
                case Move.MoveType.PawnPromotionToQueen:
                    m_pieceTypeCount[(int)pieceType]--;
                    pieceType = PieceType.Queen | (pieceType & PieceType.Black);
                    m_pieceTypeCount[(int)pieceType]++;
                    break;
                case Move.MoveType.PawnPromotionToRook:
                    m_pieceTypeCount[(int)pieceType]--;
                    pieceType = PieceType.Rook | (pieceType & PieceType.Black);
                    m_pieceTypeCount[(int)pieceType]++;
                    break;
                case Move.MoveType.PawnPromotionToBishop:
                    m_pieceTypeCount[(int)pieceType]--;
                    pieceType = PieceType.Bishop | (pieceType & PieceType.Black);
                    m_pieceTypeCount[(int)pieceType]++;
                    break;
                case Move.MoveType.PawnPromotionToKnight:
                    m_pieceTypeCount[(int)pieceType]--;
                    pieceType = PieceType.Knight | (pieceType & PieceType.Black);
                    m_pieceTypeCount[(int)pieceType]++;
                    break;
                default:
                    break;
                }
                UpdatePackedBoardAndZobristKey(move.EndPos, pieceType, move.StartPos, PieceType.None);
                m_board[move.EndPos]   = pieceType;
                m_board[move.StartPos] = PieceType.None;
                m_pieceTypeCount[(int)oldPieceType]--;
                switch(pieceType) {
                case PieceType.King | PieceType.Black:
                    m_blackKingPos = move.EndPos;
                    if (move.StartPos == 59) {
                        m_blackKingMoveCount++;
                    }
                    break;
                case PieceType.King | PieceType.White:
                    m_whiteKingPos = move.EndPos;
                    if (move.StartPos == 3) {
                        m_whiteKingMoveCount++;
                    }
                    break;
                case PieceType.Rook | PieceType.Black:
                    if (move.StartPos == 56) {
                        m_leftBlackRookMoveCount++;
                    } else if (move.StartPos == 63) {
                        m_rightBlackRookMoveCount++;
                    }
                    break;
                case PieceType.Rook | PieceType.White:
                    if (move.StartPos == 0) {
                        m_leftWhiteRookMoveCount++;
                    } else if (move.StartPos == 7) {
                        m_rightWhiteRookMoveCount++;
                    }
                    break;
                case PieceType.Pawn | PieceType.White:
                case PieceType.Pawn | PieceType.Black:
                    delta = move.StartPos - move.EndPos;
                    if (delta == -16 || delta == 16) {
                        m_possibleEnPassantPos = move.EndPos + (delta >> 1); // Position behind the pawn
                    }
                    break;
                }
                break;
            }
            MoveHistory.UpdateCurrentPackedBoard(ComputeBoardExtraInfo(addRepetitionInfo: false));
            retVal        = MoveHistory.AddCurrentPackedBoard(ZobristKey, isPawnMoveOrPieceEaten);
            CurrentPlayer = CurrentPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
            return retVal;
        }

        public void UndoMoveNoLog(Move move) {
            PieceType pieceType;
            PieceType originalPieceType;
            int       oldPiecePos;
            
            MoveHistory.RemoveLastMove(ZobristKey);
            pieceType = m_board[move.EndPos];
            switch(move.Type & Move.MoveType.MoveTypeMask) {
            case Move.MoveType.Castle:
                UpdatePackedBoardAndZobristKey(move.StartPos, pieceType, move.EndPos, PieceType.None);
                m_board[move.StartPos] = pieceType;
                m_board[move.EndPos]   = PieceType.None;
                if ((pieceType & PieceType.Black) != 0) {
                    if (move.EndPos == 57) {
                        UpdatePackedBoardAndZobristKey(56, m_board[58], 58, PieceType.None);
                        m_board[56] = m_board[58];
                        m_board[58] = PieceType.None;
                    } else {
                        UpdatePackedBoardAndZobristKey(63, m_board[60], 60, PieceType.None);
                        m_board[63] = m_board[60];
                        m_board[60] = PieceType.None;
                    }
                    m_isBlackCastled = false;
                    m_blackKingPos   = move.StartPos;
                } else {
                    if (move.EndPos == 1) {
                        UpdatePackedBoardAndZobristKey(0, m_board[2], 2, PieceType.None);
                        m_board[0] = m_board[2];
                        m_board[2] = PieceType.None;
                    } else {
                        UpdatePackedBoardAndZobristKey(7, m_board[4], 4, PieceType.None);
                        m_board[7] = m_board[4];
                        m_board[4] = PieceType.None;
                    }
                    m_isWhiteCastled = false;
                    m_whiteKingPos   = move.StartPos;
                }
                break;
            case Move.MoveType.EnPassant:
                UpdatePackedBoardAndZobristKey(move.StartPos, pieceType, move.EndPos, PieceType.None);
                m_board[move.StartPos] = pieceType;
                m_board[move.EndPos]   = PieceType.None;
                originalPieceType      = PieceType.Pawn | (((pieceType & PieceType.Black) == 0) ? PieceType.Black : PieceType.White);
                oldPiecePos            = (move.StartPos & 56) + (move.EndPos & 7);
                UpdatePackedBoardAndZobristKey(oldPiecePos, originalPieceType);
                m_board[oldPiecePos] = originalPieceType;
                m_pieceTypeCount[(int)originalPieceType]++;
                break;
            default:
                // PawnPromotion To or normal moves
                originalPieceType = move.OriginalPiece;
                switch(move.Type & Move.MoveType.MoveTypeMask) {
                case Move.MoveType.PawnPromotionToQueen:
                case Move.MoveType.PawnPromotionToRook:
                case Move.MoveType.PawnPromotionToBishop:
                case Move.MoveType.PawnPromotionToKnight:
                    m_pieceTypeCount[(int)pieceType]--;
                    pieceType = PieceType.Pawn | (pieceType & PieceType.Black);
                    m_pieceTypeCount[(int)pieceType]++;
                    break;
                default:
                    break;
                }
                UpdatePackedBoardAndZobristKey(move.StartPos, pieceType, move.EndPos, originalPieceType);
                m_board[move.StartPos] = pieceType;
                m_board[move.EndPos]   = originalPieceType;
                m_pieceTypeCount[(int)originalPieceType]++;
                switch(pieceType) {
                case PieceType.King | PieceType.Black:
                    m_blackKingPos = move.StartPos;
                    if (move.StartPos == 59) {
                        m_blackKingMoveCount--;
                    }
                    break;
                case PieceType.King:
                    m_whiteKingPos = move.StartPos;
                    if (move.StartPos == 3) {
                        m_whiteKingMoveCount--;
                    }
                    break;
                case PieceType.Rook | PieceType.Black:
                    if (move.StartPos == 56) {
                        m_leftBlackRookMoveCount--;
                    } else if (move.StartPos == 63) {
                        m_rightBlackRookMoveCount--;
                    }
                    break;
                case PieceType.Rook:
                    if (move.StartPos == 0) {
                        m_leftWhiteRookMoveCount--;
                    } else if (move.StartPos == 7) {
                        m_rightWhiteRookMoveCount--;
                    }
                    break;
                }
                break;
            }
            m_possibleEnPassantPos = m_pPossibleEnPassantPosStack.Pop();
            CurrentPlayer          = CurrentPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
        }

        public bool IsEnoughPieceForCheckMate() {
            bool retVal;
            int  bigPieceCount;
            int  whiteBishop;
            int  blackBishop;
            int  whiteKnight;
            int  blackKnight;
            
            if  (m_pieceTypeCount[(int)(PieceType.Pawn | PieceType.White)] != 0 ||
                 m_pieceTypeCount[(int)(PieceType.Pawn | PieceType.Black)] != 0) {
                 retVal = true;
            } else {
                bigPieceCount = m_pieceTypeCount[(int)(PieceType.Queen | PieceType.White)] +
                                m_pieceTypeCount[(int)(PieceType.Queen | PieceType.Black)] +
                                m_pieceTypeCount[(int)(PieceType.Rook  | PieceType.White)] +
                                m_pieceTypeCount[(int)(PieceType.Rook  | PieceType.Black)];
                if (bigPieceCount != 0) {
                    retVal = true;
                } else {
                    whiteBishop = m_pieceTypeCount[(int)(PieceType.Bishop | PieceType.White)];
                    blackBishop = m_pieceTypeCount[(int)(PieceType.Bishop | PieceType.Black)];
                    whiteKnight = m_pieceTypeCount[(int)(PieceType.Knight | PieceType.White)];
                    blackKnight = m_pieceTypeCount[(int)(PieceType.Knight | PieceType.Black)];
                    if ((whiteBishop + whiteKnight) >= 2 || (blackBishop + blackKnight) >= 2) {
                        // Two knights is typically impossible... but who knows!
                        retVal = true;
                    } else {
                        retVal = false;
                    }
                }
            }
            return retVal;
        }

        public GameResult GetCurrentResult(RepeatResult repeatResult) {
            GameResult  retVal;
            List<Move>  moveList;
            PlayerColor playerColor;

            switch(repeatResult) {
            case RepeatResult.ThreeFoldRepeat:
                retVal = GameResult.ThreeFoldRepeat;
                break;
            case RepeatResult.FiftyRuleRepeat:
                retVal = GameResult.FiftyRuleRepeat;
                break;
            default:
                if (IsEnoughPieceForCheckMate()) {
                    playerColor = CurrentPlayer;
                    moveList    = EnumMoveList(playerColor);
                    if (IsCheck(playerColor)) {
                        retVal = (moveList.Count == 0) ? GameResult.Mate : GameResult.Check;
                    } else {
                        retVal = (moveList.Count == 0) ? GameResult.TieNoMove : GameResult.OnGoing;
                    }
                } else {
                    retVal = GameResult.TieNoMatePossible;
                }
                break;
            }
            return retVal;
        }

        public GameResult GetCurrentResult() => GetCurrentResult(MoveHistory.CurrentRepeatResult(ZobristKey));

        public GameResult DoMove(MoveExt move) {
            GameResult   retVal;
            RepeatResult repeatResult;
            
            repeatResult = DoMoveNoLog(move.Move);
            retVal       = GetCurrentResult(repeatResult);
            MovePosStack.AddMove(move);
            return retVal;
        }

        public void UndoMove() {
            UndoMoveNoLog(MovePosStack.CurrentMove.Move);
            MovePosStack.MoveToPrevious();
        }

        public GameResult RedoMove() {
            GameResult   retVal;
            RepeatResult repeatResult;
            
            repeatResult = DoMoveNoLog(MovePosStack.NextMove.Move);
            retVal       = GetCurrentResult(repeatResult);
            MovePosStack.MoveToNext();
            return retVal;
        }

        public void SetUndoRedoPosition(int pos) {
            int curPos;
            
            curPos = MovePosStack.PositionInList;
            while (curPos > pos) {
                UndoMove();
                curPos--;
            }
            while (curPos < pos) {
                RedoMove();
                curPos++;
            }
        }

        public int WhitePieceCount {
            get {
                int retVal = 0;
                
                for (int i = 1; i < 7; i++) {
                    retVal += m_pieceTypeCount[i];
                }
                return retVal;
            }
        }
        public int BlackPieceCount {
            get {
                int retVal = 0;
                
                for (int i = 9; i < 15; i++) {
                    retVal += m_pieceTypeCount[i];
                }
                return retVal;
            }
        }

        // Enumerates the attacking position using arrays of possible position and two possible enemy pieces
        private int EnumTheseAttackPos(List<byte>? attackPosList, int[][] caseMoveList, PieceType pieceType1, PieceType pieceType2) {
            int       retVal = 0;
            PieceType pieceType;
            
            foreach (int[] moveList in caseMoveList) {
                foreach (int newPos in moveList) {
                    pieceType = m_board[newPos];
                    if (pieceType != PieceType.None) {
                        if (pieceType == pieceType1 ||
                            pieceType == pieceType2) {
                            retVal++;
                            attackPosList?.Add((byte)newPos);
                        }
                        break;
                    }                    
                }
            }
            return retVal;
        }

        // Enumerates the attacking position using an array of possible position and one possible enemy piece
        private int EnumTheseAttackPos(List<byte>? attackPosList, int[] caseMoveList, PieceType pieceType) {
            int retVal = 0;
            
            foreach (int newPos in caseMoveList) {
                if (m_board[newPos] == pieceType) {
                    retVal++;
                    attackPosList?.Add((byte)newPos);
                }
            }
            return retVal;
        }

        // Enumerates all position which can attack a given position
        private int EnumAttackPos(PlayerColor playerColor, int pos, List<byte>? attackPosList) {
            int       retVal;
            PieceType pieceColor;
            PieceType enemyQueen;
            PieceType enemyRook;
            PieceType enemyKing;
            PieceType enemyBishop;
            PieceType enemyKnight;
            PieceType enemyPawn;
                                          
            pieceColor  = (playerColor == PlayerColor.Black) ? PieceType.White : PieceType.Black;
            enemyQueen  = PieceType.Queen  | pieceColor;
            enemyRook   = PieceType.Rook   | pieceColor;
            enemyKing   = PieceType.King   | pieceColor;
            enemyBishop = PieceType.Bishop | pieceColor;
            enemyKnight = PieceType.Knight | pieceColor;
            enemyPawn   = PieceType.Pawn   | pieceColor;
            retVal      = EnumTheseAttackPos(attackPosList, s_caseMoveDiagonal[pos], enemyQueen, enemyBishop);
            retVal     += EnumTheseAttackPos(attackPosList, s_caseMoveLine[pos],     enemyQueen, enemyRook);
            retVal     += EnumTheseAttackPos(attackPosList, s_caseMoveKing[pos],     enemyKing);
            retVal     += EnumTheseAttackPos(attackPosList, s_caseMoveKnight[pos],   enemyKnight);
            retVal     += EnumTheseAttackPos(attackPosList,
                                             (playerColor == PlayerColor.Black) ? s_caseWhitePawnCanAttackFrom[pos] : s_caseBlackPawnCanAttackFrom[pos],
                                             enemyPawn);
            return retVal;
        }

        private bool IsCheck(PlayerColor playerColor, int kingPos) => EnumAttackPos(playerColor, kingPos, null) != 0;

        public bool IsCheck(PlayerColor playerColor) => IsCheck(playerColor, (playerColor == PlayerColor.Black) ? m_blackKingPos : m_whiteKingPos);

        // Evaluates a board. The number of point is greater than 0 if white is in advantage, less than 0 if black is.
        public int Points<TSetting>(TSetting      searchEngineSetting,
                                    PlayerColor   playerColor,
                                    int           moveCountDelta,
                                    AttackPosInfo whiteAttackPosInfo,
                                    AttackPosInfo blackAttackPosInfo) where TSetting : SearchEngineSetting {
            int              retVal;
            IBoardEvaluation boardEval;
            AttackPosInfo    tAttackPosInfo;

            if (searchEngineSetting is ChessSearchSetting chessSearchSetting) {
                if (playerColor == PlayerColor.White) {
                    boardEval      = chessSearchSetting.WhiteBoardEvaluator;
                    tAttackPosInfo = whiteAttackPosInfo;
                } else {
                    boardEval                      = chessSearchSetting.BlackBoardEvaluator;
                    tAttackPosInfo.PiecesAttacked  = -blackAttackPosInfo.PiecesAttacked;
                    tAttackPosInfo.PiecesDefending = -blackAttackPosInfo.PiecesDefending;
                }
                retVal = boardEval.Points(m_board, m_pieceTypeCount, tAttackPosInfo, m_whiteKingPos, m_blackKingPos, m_isWhiteCastled, m_isBlackCastled, moveCountDelta);
            } else {
                throw new InvalidProgramException("Coding error");
            }
            return retVal;
        }

        // Add a move to the move list if the move doesn't provokes the king to be attacked.
        private void AddIfNotCheck(PlayerColor playerColor, int startPos, int endPos, Move.MoveType moveType, List<Move>? movePosList) {
            PieceType newPiece;
            PieceType oldPiece;
            Move      move;
            bool      isCheck;
            
            oldPiece          = m_board[endPos];
            newPiece          = m_board[startPos];
            m_board[endPos]   = newPiece;
            m_board[startPos] = PieceType.None;
            isCheck           = ((newPiece & PieceType.PieceMask) == PieceType.King) ? IsCheck(playerColor, endPos) : IsCheck(playerColor);
            m_board[startPos] = m_board[endPos];
            m_board[endPos]   = oldPiece;
            if (!isCheck) {
                move.OriginalPiece = m_board[endPos];
                move.StartPos      = (byte)startPos;
                move.EndPos        = (byte)endPos;
                move.Type          = moveType;
                if (m_board[endPos] != PieceType.None || moveType == Move.MoveType.EnPassant) {
                    move.Type |= Move.MoveType.PieceEaten;
                    m_posInfo.PiecesAttacked++;
                }
                movePosList?.Add(move);
            }
        }

        // Add a pawn promotion series of moves to the move list if the move doesn't provokes the king to be attacked.
        private void AddPawnPromotionIfNotCheck(PlayerColor playerColor, int startPos, int endPos, List<Move>? listMovePos) {
            AddIfNotCheck(playerColor, startPos, endPos, Move.MoveType.PawnPromotionToQueen,  listMovePos);
            AddIfNotCheck(playerColor, startPos, endPos, Move.MoveType.PawnPromotionToRook,   listMovePos);
            AddIfNotCheck(playerColor, startPos, endPos, Move.MoveType.PawnPromotionToBishop, listMovePos);
            AddIfNotCheck(playerColor, startPos, endPos, Move.MoveType.PawnPromotionToKnight, listMovePos);
        }

        private bool AddMoveIfEnemyOrEmpty(PlayerColor playerColor, int startPos, int endPos, List<Move>? listMovePos) {
            bool        retVal;
            PieceType   oldPiece;
            
            retVal   = (m_board[endPos] == PieceType.None);
            oldPiece = m_board[endPos];
            if (retVal ||((oldPiece & PieceType.Black) != 0) != (playerColor == PlayerColor.Black)) {
                AddIfNotCheck(playerColor, startPos, endPos, Move.MoveType.Normal, listMovePos);
            } else {
                m_posInfo.PiecesDefending++;
            }
            return retVal;
        }

        // Enumerates the castling move
        private void EnumCastleMove(PlayerColor playerColor, List<Move>? movePosList) {
            if (playerColor == PlayerColor.Black) {
                if (!m_isBlackCastled) {
                    if (m_blackKingMoveCount == 0) {
                        if (m_leftBlackRookMoveCount == 0 &&
                            m_board[57] == PieceType.None &&
                            m_board[58] == PieceType.None &&
                            m_board[56] == (PieceType.Rook | PieceType.Black)) {
                            if (EnumAttackPos(playerColor, 58, null) == 0 &&
                                EnumAttackPos(playerColor, 59, null) == 0) {
                                AddIfNotCheck(playerColor, 59, 57, Move.MoveType.Castle, movePosList);
                            }
                        }
                        if (m_rightBlackRookMoveCount == 0 &&
                            m_board[60] == PieceType.None  &&
                            m_board[61] == PieceType.None  &&
                            m_board[62] == PieceType.None  &&
                            m_board[63] == (PieceType.Rook | PieceType.Black)) {
                            if (EnumAttackPos(playerColor, 59, null) == 0 &&
                                EnumAttackPos(playerColor, 60, null) == 0) {
                                AddIfNotCheck(playerColor, 59, 61, Move.MoveType.Castle, movePosList);
                            }
                        }
                    }
                }
            } else {
                if (!m_isWhiteCastled) {
                    if (m_whiteKingMoveCount == 0) {
                        if (m_leftWhiteRookMoveCount == 0 &&
                            m_board[1] == PieceType.None  &&
                            m_board[2] == PieceType.None  &&
                            m_board[0] == (PieceType.Rook | PieceType.White)) {
                            if (EnumAttackPos(playerColor, 2, null) == 0 &&
                                EnumAttackPos(playerColor, 3, null) == 0) {                                
                                AddIfNotCheck(playerColor, 3, 1, Move.MoveType.Castle, movePosList);
                            }
                        }
                        if (m_rightWhiteRookMoveCount == 0 &&
                            m_board[4] == PieceType.None   &&
                            m_board[5] == PieceType.None   &&
                            m_board[6] == PieceType.None   &&
                            m_board[7] == (PieceType.Rook | PieceType.White)) {
                            if (EnumAttackPos(playerColor, 3, null) == 0 &&
                                EnumAttackPos(playerColor, 4, null) == 0) {
                                AddIfNotCheck(playerColor, 3, 5, Move.MoveType.Castle, movePosList);
                            }
                        }
                    }
                }
            }
        }

        // Enumerates the move a specified pawn can do
        private void EnumPawnMove(PlayerColor playerColor, int startPos, List<Move>? movePosList) {
            int  dir;
            int  newPos;
            int  newColPos;
            int  rowPos;
            bool canMove2Case;
            
            rowPos       = (startPos >> 3);
            canMove2Case = (playerColor == PlayerColor.Black) ? (rowPos == 6) : (rowPos == 1);
            dir          = (playerColor == PlayerColor.Black) ? -8 : 8;
            newPos       = startPos + dir;
            if (newPos >= 0 && newPos < 64) {
                if (m_board[newPos] == PieceType.None) {
                    rowPos = (newPos >> 3);
                    if (rowPos == 0 || rowPos == 7) {
                        AddPawnPromotionIfNotCheck(playerColor, startPos, newPos, movePosList);
                    } else {
                        AddIfNotCheck(playerColor, startPos, newPos, Move.MoveType.Normal, movePosList);
                    }
                    if (canMove2Case && m_board[newPos+dir] == PieceType.None) {
                        AddIfNotCheck(playerColor, startPos, newPos+dir, Move.MoveType.Normal, movePosList);
                    }
                }
            }
            newPos = startPos + dir;
            if (newPos >= 0 && newPos < 64) {
                newColPos = newPos & 7;
                rowPos    = (newPos >> 3);
                if (newColPos != 0 && m_board[newPos - 1] != PieceType.None) {
                    if (((m_board[newPos - 1] & PieceType.Black) == 0) == (playerColor == PlayerColor.Black)) {
                        if (rowPos == 0 || rowPos == 7) {
                            AddPawnPromotionIfNotCheck(playerColor, startPos, newPos - 1, movePosList);
                        } else {
                            AddIfNotCheck(playerColor, startPos, newPos - 1, Move.MoveType.Normal, movePosList);
                        }
                    } else {
                        m_posInfo.PiecesDefending++;
                    }
                }
                if (newColPos != 7 && m_board[newPos + 1] != PieceType.None) {
                    if (((m_board[newPos + 1] & PieceType.Black) == 0) == (playerColor == PlayerColor.Black)) {
                        if (rowPos == 0 || rowPos == 7) {
                            AddPawnPromotionIfNotCheck(playerColor, startPos, newPos + 1, movePosList);
                        } else {
                            AddIfNotCheck(playerColor, startPos, newPos + 1, Move.MoveType.Normal, movePosList);
                        }
                    } else {
                        m_posInfo.PiecesDefending++;
                    }
                }
            }            
        }

        // Enumerates the en passant move
        private void EnumEnPassant(PlayerColor playerColor, List<Move>? movePosList) {
            int       colPos;
            PieceType attackingPawn;
            PieceType pawnInDanger;
            int       posBehindPawn;
            int       posPawnInDanger;
            
            if (m_possibleEnPassantPos != 0) {
                posBehindPawn = m_possibleEnPassantPos;
                if (playerColor == PlayerColor.White) {
                    posPawnInDanger = posBehindPawn - 8;
                    attackingPawn   = PieceType.Pawn | PieceType.White;
                } else {
                    posPawnInDanger = posBehindPawn + 8;
                    attackingPawn   = PieceType.Pawn | PieceType.Black;
                }
                pawnInDanger = m_board[posPawnInDanger];
                // Check if there is an attacking pawn at the left
                colPos       = posPawnInDanger & 7;
                if (colPos > 0 && m_board[posPawnInDanger - 1] == attackingPawn) {
                    m_board[posPawnInDanger] = PieceType.None;
                    AddIfNotCheck(playerColor,
                                  posPawnInDanger - 1,
                                  posBehindPawn,
                                  Move.MoveType.EnPassant,
                                  movePosList);
                    m_board[posPawnInDanger] = pawnInDanger;
                }
                if (colPos < 7 && m_board[posPawnInDanger+1] == attackingPawn) {
                    m_board[posPawnInDanger] = PieceType.None;
                    AddIfNotCheck(playerColor,
                                  posPawnInDanger + 1,
                                  posBehindPawn,
                                  Move.MoveType.EnPassant,
                                  movePosList);
                    m_board[posPawnInDanger] = pawnInDanger;
                }
            }
        }

        // Enumerates the move a specified piece can do using the pre-compute move array
        private void EnumFromArray(PlayerColor playerColor, int startPos, int[][] moveListForThisCase, List<Move>? listMovePos) {
            foreach (int[] movePosForThisDiag in moveListForThisCase) {
                foreach (int newPos in movePosForThisDiag) {
                    if (!AddMoveIfEnemyOrEmpty(playerColor, startPos, newPos, listMovePos)) {
                        break;
                    }
                }
            }
        }
        private void EnumFromArray(PlayerColor playerColor, int startPos, int[] moveListForThisCase, List<Move>? listMovePos) {
            foreach (int newPos in moveListForThisCase) {
                AddMoveIfEnemyOrEmpty(playerColor, startPos, newPos, listMovePos);
            }
        }

        // Enumerates all the possible moves for a player
        public List<Move>? EnumMoveList(PlayerColor playerColor, bool isMoveListNeeded, out AttackPosInfo attackPosInfo) {
            List<Move>? retVal;
            PieceType   pieceType;
            bool        isBlackToMove;

            m_posInfo.PiecesAttacked  = 0;
            m_posInfo.PiecesDefending = 0;
            retVal        = (isMoveListNeeded) ? new List<Move>(256) : null;
            isBlackToMove = (playerColor == PlayerColor.Black);
            for (int i = 0; i < 64; i++) {
                pieceType = m_board[i];
                if (pieceType != PieceType.None && ((pieceType & PieceType.Black) != 0) == isBlackToMove) {
                    switch(pieceType & PieceType.PieceMask) {
                    case PieceType.Pawn:
                        EnumPawnMove(playerColor, i, retVal);
                        break;
                    case PieceType.Knight:
                        EnumFromArray(playerColor, i, s_caseMoveKnight[i], retVal);
                        break;
                    case PieceType.Bishop:
                        EnumFromArray(playerColor, i, s_caseMoveDiagonal[i], retVal);
                        break;
                    case PieceType.Rook:
                        EnumFromArray(playerColor, i, s_caseMoveLine[i], retVal);
                        break;
                    case PieceType.Queen:
                        EnumFromArray(playerColor, i, s_caseMoveDiagLine[i], retVal);
                        break;
                    case PieceType.King:
                        EnumFromArray(playerColor, i, s_caseMoveKing[i], retVal);
                        break;
                    }
                }
            }
            EnumCastleMove(playerColor, retVal);
            EnumEnPassant(playerColor, retVal);
            attackPosInfo = m_posInfo;
            return retVal;
        }
        public List<Move> EnumMoveList(PlayerColor playerColor) => EnumMoveList(playerColor, true, out _)!;

        public static void CancelSearch() => SearchEngine<ChessGameBoardAdaptor,Move>.CancelSearch();

        // Find the best move for the given player
        public bool FindBestMove<T>(ChessBoard.PlayerColor  playerColor,
                                    ChessSearchSetting      chessSearchSetting,
                                    Action<T,object?>?      foundMoveAction,
                                    T                       cookie)
            => SearchEngine<ChessGameBoardAdaptor,Move>.FindBestMove(m_trace,
                                                                     m_rnd,
                                                                     m_repRnd,
                                                                     m_boardAdaptor,
                                                                     chessSearchSetting,
                                                                     (int)playerColor,
                                                                     isMaximizing: playerColor == PlayerColor.White,
                                                                     foundMoveAction,
                                                                     cookie);

        // returns: None or a combination of Queen, Rook, Bishop, Knight and Pawn
        public ValidPawnPromotion FindValidPawnPromotion(PlayerColor playerColor, int startPos, int endPos) {
            ValidPawnPromotion retVal = ValidPawnPromotion.None;
            List<Move>         moveList;

            moveList = EnumMoveList(playerColor);
            foreach (Move move in moveList) {
                if (move.StartPos == startPos && move.EndPos == endPos) {
                    switch(move.Type & Move.MoveType.MoveTypeMask) {
                    case Move.MoveType.PawnPromotionToQueen:
                        retVal |= ValidPawnPromotion.Queen;
                        break;
                    case Move.MoveType.PawnPromotionToRook:
                        retVal |= ValidPawnPromotion.Rook;
                        break;
                    case Move.MoveType.PawnPromotionToBishop:
                        retVal |= ValidPawnPromotion.Bishop;
                        break;
                    case Move.MoveType.PawnPromotionToKnight:
                        retVal |= ValidPawnPromotion.Knight;
                        break;
                    default:
                        break;
                    }
                }
            }
            return retVal;
        }        

        public Move FindIfValid(PlayerColor playerColor, int startPos, int endPos) {
            Move       retVal;
            List<Move> moveList;
            int        index;

            moveList = EnumMoveList(playerColor);
            index    = moveList.FindIndex(x => x.StartPos == startPos && x.EndPos == endPos);
            if (index == -1) {
                retVal.StartPos      = 255;
                retVal.EndPos        = 255;
                retVal.OriginalPiece = PieceType.None;
                retVal.Type          = Move.MoveType.Normal;
            } else {
                retVal = moveList[index];
            }
            return retVal;
        }
        public bool IsMoveValid(PlayerColor playerColor, Move move) {
            bool        retVal;
            List<Move>  moveList;

            moveList = EnumMoveList(playerColor);
            retVal   = moveList.FindIndex(x => x.StartPos == move.StartPos && x.EndPos == move.EndPos) != -1;
            return retVal;
        }
        public bool IsMoveValid(Move move) => IsMoveValid(CurrentPlayer, move);

        public bool FindBookMove(Book book, ChessSearchSetting chessSearchSetting, PlayerColor playerColor, MoveExt[] prevMoves, out Move move) {
            bool    retVal;
            int     packedMove;
            Random? rnd;
            
            if (chessSearchSetting.RandomMode == RandomMode.Off) {
                rnd = null;
            } else if (chessSearchSetting.RandomMode == RandomMode.OnRepetitive) {
                rnd = m_repRnd;
            } else {
                rnd = m_rnd;
            }
            move.OriginalPiece = PieceType.None;
            move.StartPos      = 255;
            move.EndPos        = 255;
            move.Type          = Move.MoveType.Normal;
            packedMove         = book.FindMoveInBook(prevMoves, rnd);
            if (packedMove == -1) {
                retVal = false;
            } else {
                move       = FindIfValid(playerColor, packedMove & 255, packedMove >> 8);
                move.Type |= Move.MoveType.MoveFromBook;
                retVal     = (move.StartPos != 255);
            }
            return retVal;
        }

        public void UndoAllMoves() {
            while (MovePosStack.PositionInList != -1) {
                UndoMove();
            }
        }
    }
}
