using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using SrcChess2.Core;

namespace SrcChess2.PgnParsing {
    //
    //  PGN BNF
    //
    //  <PGN-database>           ::= {<PGN-game>}
    //  <PGN-game>               ::= <tag-section> <movetext-section>
    //  <tag-section>            ::= {<tag-pair>}
    //  <tag-pair>               ::= '[' <tag-name> <tag-value> ']'
    //  <tag-name>               ::= <identifier>
    //  <tag-value>              ::= <string>
    //  <movetext-section>       ::= <element-sequence> <game-termination>
    //  <element-sequence>       ::= {<element>}
    //  <element>                ::= <move-number-indication> | <SAN-move> | <numeric-annotation-glyph>
    //  <move-number-indication> ::= Integer {'.'}
    //  <recursive-variation>    ::= '(' <element-sequence> ')'
    //  <game-termination>       ::= '1-0' | '0-1' | '1/2-1/2' | '*'

    /// <summary>
    /// Parser exception
    /// </summary>
    /// <remarks>
    /// Constructor
    /// </remarks>
    /// <param name="errTxt">      Error Message</param>
    /// <param name="codeInError"> Code in error</param>
    /// <param name="ex">          Inner exception</param>
    [Serializable]
    public class PgnParserException(string? errTxt, string? codeInError, Exception? ex) : Exception(errTxt, ex) {
        
        /// <summary>Code which is in error</summary>
        public string? CodeInError { get; } = codeInError;         /// <summary>Array of move position</summary>
        public short[]? MoveList { get; set; } = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="errTxt">       Error Message</param>
        /// <param name="codeInError">  Code in error</param>
        public PgnParserException(string? errTxt, string? codeInError) : this(errTxt, codeInError, ex: null) {}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="errTxt">       Error Message</param>
        public PgnParserException(string? errTxt) : this(errTxt, codeInError: "", ex: null) {}

        /// <summary>
        /// Constructor
        /// </summary>
        public PgnParserException() : this(errTxt: "", codeInError: "", ex: null) {}

    } // Class PgnParserException

    /// <summary>
    /// Class implementing the parsing of a PGN document. PGN is a standard way of recording chess games.
    /// </summary>
    public class PgnParser {

        /// <summary>
        /// Parsing Phase
        /// </summary>
        public enum ParsingPhase {
            /// <summary>No phase set yet</summary>
            None         = 0,
            /// <summary>Openning a file</summary>
            OpeningFile  = 1,
            /// <summary>Reading the file content into memory</summary>
            ReadingFile  = 2,
            /// <summary>Raw parsing the PGN file</summary>
            RawParsing   = 3,
            /// <summary>Creating the book</summary>
            CreatingBook = 10,
            /// <summary>Processing is finished</summary>
            Finished     = 255
        }

        /// <summary>true to cancel the parsing job</summary>
        private static bool         m_isJobCancelled;
        /// <summary>Board use to play as we decode</summary>
        private readonly ChessBoard m_chessBoard;
        /// <summary>true to diagnose the parser. This generate exception when a move cannot be resolved</summary>
        private readonly bool       m_isDiagnoseOn;
        /// <summary>PGN Lexical Analyser</summary>
        private PgnLexical?         m_pgnLexical;

        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="isDiagnoseOn"> true to diagnose the parser</param>
        public PgnParser(bool isDiagnoseOn) {
            m_chessBoard   = new ChessBoard();
            m_isDiagnoseOn = isDiagnoseOn;
            m_pgnLexical   = null;
        }

        /// <summary>
        /// Class Ctor
        /// </summary>
        /// <param name="chessBoard"> Chessboard to use</param>
        public PgnParser(ChessBoard chessBoard) {
            m_chessBoard   = chessBoard;
            m_isDiagnoseOn = false;
            m_pgnLexical   = null;
        }

        /// <summary>
        /// Initialize the parser using the content of a PGN file
        /// </summary>
        /// <param name="fileName"> File name</param>
        /// <returns>true if succeed, false if failed</returns>
        public bool InitFromFile(string fileName) {
            bool retVal;

            m_pgnLexical ??= new PgnLexical();
            retVal         = m_pgnLexical.InitFromFile(fileName);
            return retVal;
        }

        /// <summary>
        /// Initialize the parser using a PGN text
        /// </summary>
        /// <param name="text">  PGN Text</param>
        public void InitFromString(string text) {
            m_pgnLexical ??= new PgnLexical();
            m_pgnLexical.InitFromString(text);
        }

        /// <summary>
        /// Initialize from a PGN buffer object
        /// </summary>
        /// <param name="pgnLexical">    PGN Lexical Analyser</param>
        public void InitFromPgnBuffer(PgnLexical pgnLexical) => m_pgnLexical = pgnLexical;

        /// <summary>
        /// PGN buffer
        /// </summary>
        public PgnLexical? PgnLexical => m_pgnLexical;

        /// <summary>
        /// Return the code of the current game
        /// </summary>
        /// <returns>
        /// Current game
        /// </returns>
        private string GetCodeInError(long startPos, int length) => m_pgnLexical!.GetStringAtPos(startPos, length)!;

        /// <summary>
        /// Return the code of the current game
        /// </summary>
        /// <param name="tok">  Token</param>
        /// <returns>
        /// Current game
        /// </returns>
        private string GetCodeInError(PgnLexical.Token tok) => m_pgnLexical!.GetStringAtPos(tok.StartPos, tok.Size)!;

        /// <summary>
        /// Return the code of the current game
        /// </summary>
        /// <param name="pgnGame">    PGN game</param>
        /// <returns>
        /// Current game
        /// </returns>
        private string GetCodeInError(PgnGame pgnGame) => GetCodeInError(pgnGame.StartingPos, pgnGame.Length);

        /// <summary>
        /// Callback for 
        /// </summary>
        /// <param name="cookie">        Callback cookie</param>
        /// <param name="phase">         Parsing phase OpeningFile,ReadingFile,RawParsing,AnalysingMoves</param>
        /// <param name="fileIndex">     File index</param>
        /// <param name="fileCount">     Number of files to parse</param>
        /// <param name="fileName">      File name</param>
        /// <param name="gameProcessed"> Game processed since the last update</param>
        /// <param name="gameCount">     Game count</param>
        public delegate void DelProgressCallBack(object? cookie, ParsingPhase phase, int fileIndex, int fileCount, string? fileName, int gameProcessed, int gameCount);

        /// <summary>
        /// Decode a move
        /// </summary>
        /// <param name="pgnGame">  PGN game</param>
        /// <param name="pos">      Move position</param>
        /// <param name="startCol"> Returns the starting column found in move if specified (-1 if not)</param>
        /// <param name="startRow"> Returns the starting row found in move if specified (-1 if not)</param>
        /// <param name="endPos">   Returns the ending position of the move</param>
        private void DecodeMove(PgnGame pgnGame, string pos, out int startCol, out int startRow, out int endPos) {
            char chr1;
            char chr2;
            char chr3;
            char chr4;

            switch(pos.Length) {
            case 2:
                chr1 = pos[0];
                chr2 = pos[1];
                if (chr1 < 'a' || chr1 > 'h' ||
                    chr2 < '1' || chr2 > '8') {
                    throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
                }
                startCol = -1;
                startRow = -1;
                endPos   = (7 - (chr1 - 'a')) + ((chr2 - '1') << 3);
                break;
            case 3:
                chr1 = pos[0];
                chr2 = pos[1];
                chr3 = pos[2];
                if (chr1 >= 'a' && chr1 <= 'h') {
                    startCol = 7 - (chr1 - 'a');
                    startRow = -1;
                } else if (chr1 >= '1' && chr1 <= '8') {
                    startCol = -1;
                    startRow = (chr1 - '1');
                } else {
                    throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
                }
                if (chr2 < 'a' || chr2 > 'h' ||
                    chr3 < '1' || chr3 > '8') {
                    throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
                }
                endPos = (7 - (chr2 - 'a')) + ((chr3 - '1') << 3);
                break;
            case 4:
                chr1 = pos[0];
                chr2 = pos[1];
                chr3 = pos[2];
                chr4 = pos[3];
                if (chr1 < 'a' || chr1 > 'h' ||
                    chr2 < '1' || chr2 > '8' ||
                    chr3 < 'a' || chr3 > 'h' ||
                    chr4 < '1' || chr4 > '8') {
                    throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
                }
                startCol = 7 - (chr1 - 'a');
                startRow = (chr2 - '1');
                endPos   = (7 - (chr3 - 'a')) + ((chr4 - '1') << 3);
                break;
            default:
                throw new PgnParserException("Unable to decode position", GetCodeInError(pgnGame));
            }
        }


        /// <summary>
        /// Convert a SAN position into a moving position
        /// </summary>
        /// <param name="pgnGame">        PGN game</param>
        /// <param name="playerColor">    Color moving</param>
        /// <param name="moveText">       Move text</param>
        /// <param name="pos">            Returned moving position (-1 if error, Starting position + Ending position * 256</param>
        /// <param name="truncatedCount"> Truncated count</param>
        /// <param name="move">           Move position</param>
        private void CnvSanMoveToPosMove(PgnGame pgnGame, ChessBoard.PlayerColor playerColor, string moveText, out short pos, ref int truncatedCount, ref MoveExt move) {
            string               pureMove;
            int                  index;
            ChessBoard.PieceType pieceType;
            Move.MoveType        moveType;
            
            moveType = Move.MoveType.Normal;
            pos      = 0;
            pureMove = moveText.Replace("x", "").Replace("#", "").Replace("ep","").Replace("+", "");
            index    = pureMove.IndexOf('=');
            if (index != -1) {
                if (pureMove.Length > index + 1) {
                    switch(pureMove[index+1]) {
                    case 'Q':
                        moveType = Move.MoveType.PawnPromotionToQueen;
                        break;
                    case 'R':
                        moveType = Move.MoveType.PawnPromotionToRook;
                        break;
                    case 'B':
                        moveType = Move.MoveType.PawnPromotionToBishop;
                        break;
                    case 'N':
                        moveType = Move.MoveType.PawnPromotionToKnight;
                        break;
                    default:
                        pos = -1;
                        truncatedCount++;
                        break;
                    }
                    if (pos != -1) {
                        pureMove = pureMove[..index];
                    }
                } else {
                    pos = -1;
                    truncatedCount++;
                }
            }
        }

        /// <summary>
        /// Convert a list of SAN positions into a moving positions
        /// </summary>
        /// <param name="pgnGame">        PGN game</param>
        /// <param name="colorToPlay">    Color to play</param>
        /// <param name="rawMoveList">    Array of PGN moves</param>
        /// <param name="moves">          Returned array of moving position (Starting Position + Ending Position * 256)</param>
        /// <param name="movePosList">    Returned the list of move if not null</param>
        /// <param name="truncatedCount"> Truncated count</param>
        private void CnvSanMoveToPosMove(PgnGame pgnGame, ChessBoard.PlayerColor colorToPlay, List<string> rawMoveList, out short[] moves, List<MoveExt>? movePosList, ref int truncatedCount) {
            List<short> shortMoveList;
            MoveExt     move;

            move          = new MoveExt(ChessBoard.PieceType.None, 0, 0, Move.MoveType.Normal, "", -1, -1, 0, 0);
            shortMoveList = new List<short>(256);
            try {
                foreach (string moveTxt in rawMoveList) {
                    CnvSanMoveToPosMove(pgnGame, colorToPlay, moveTxt, out short pos, ref truncatedCount, ref move);
                    if (pos != -1) {
                        shortMoveList.Add(pos);
                        movePosList?.Add(move);
                        colorToPlay = (colorToPlay == ChessBoard.PlayerColor.Black) ? ChessBoard.PlayerColor.White : ChessBoard.PlayerColor.Black;
                    } else {
                        break;
                    }
                }
            } catch(PgnParserException ex) {
                ex.MoveList = [.. shortMoveList];
                throw;
            }
            moves = [.. shortMoveList];
        }

        /// <summary>
        /// Parse FEN definition into a board representation
        /// </summary>
        /// <param name="fenTxt">         FEN</param>
        /// <param name="colorToMove">    Return the color to move</param>
        /// <param name="boardStateMask"> Return the mask of castling info</param>
        /// <param name="enPassantPos">   Return the en passant position or 0 if none</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        private bool ParseFen(string fenTxt, out ChessBoard.PlayerColor colorToMove, out ChessBoard.BoardStateMask boardStateMask, out int enPassantPos) {
            bool                 retVal = true;
            string[]             cmds;
            string[]             rows;
            string               cmd;
            int                  pos;
            int                  linePos;
            int                  blankCount;
            ChessBoard.PieceType pieceType;
            
            boardStateMask = (ChessBoard.BoardStateMask)0;
            enPassantPos   = 0;
            colorToMove    = ChessBoard.PlayerColor.White;
            cmds           = fenTxt.Split(' ');
            if (cmds.Length != 6) {
                retVal = false;
            } else {
                rows = cmds[0].Split('/');
                if (rows.Length != 8) {
                    retVal = false;
                } else {
                    pos = 63;
                    foreach (string row in rows) {
                        linePos = 0;
                        foreach (char chr in row) {
                            pieceType = ChessBoard.PieceType.None;
                            switch(chr) {
                            case 'P':
                                pieceType = ChessBoard.PieceType.Pawn   | ChessBoard.PieceType.White;
                                break;
                            case 'N':
                                pieceType = ChessBoard.PieceType.Knight | ChessBoard.PieceType.White;
                                break;
                            case 'B':
                                pieceType = ChessBoard.PieceType.Bishop | ChessBoard.PieceType.White;
                                break;
                            case 'R':
                                pieceType = ChessBoard.PieceType.Rook   | ChessBoard.PieceType.White;
                                break;
                            case 'Q':
                                pieceType = ChessBoard.PieceType.Queen  | ChessBoard.PieceType.White;
                                break;
                            case 'K':
                                pieceType = ChessBoard.PieceType.King   | ChessBoard.PieceType.White;
                                break;
                            case 'p':
                                pieceType = ChessBoard.PieceType.Pawn   | ChessBoard.PieceType.Black;
                                break;
                            case 'n':
                                pieceType = ChessBoard.PieceType.Knight | ChessBoard.PieceType.Black;
                                break;
                            case 'b':
                                pieceType = ChessBoard.PieceType.Bishop | ChessBoard.PieceType.Black;
                                break;
                            case 'r':
                                pieceType = ChessBoard.PieceType.Rook   | ChessBoard.PieceType.Black;
                                break;
                            case 'q':
                                pieceType = ChessBoard.PieceType.Queen  | ChessBoard.PieceType.Black;
                                break;
                            case 'k':
                                pieceType = ChessBoard.PieceType.King   | ChessBoard.PieceType.Black;
                                break;
                            default:
                                if (chr >= '1' && chr <= '8') {
                                     blankCount = int.Parse(chr.ToString());
                                     if (blankCount + linePos <= 8) {
                                        for (int i = 0; i < blankCount; i++) {
                                            m_chessBoard[pos--] = ChessBoard.PieceType.None;
                                        }
                                        linePos += blankCount;
                                    }
                                } else {
                                    retVal = false;
                                }
                                break;
                            }
                            if (retVal && pieceType != ChessBoard.PieceType.None) {
                                if (linePos < 8) {
                                    m_chessBoard[pos--] = pieceType;
                                    linePos++;
                                } else {
                                    retVal = false;
                                }
                            }
                        }
                        if (linePos != 8) {
                            retVal = false;
                        }
                    }
                    if (retVal) {
                        cmd = cmds[1];
                        if (cmd == "w") {
                            colorToMove = ChessBoard.PlayerColor.White;
                        } else if (cmd == "b") {
                            colorToMove = ChessBoard.PlayerColor.Black;
                        } else {
                            retVal = false;
                        }
                        cmd = cmds[2];
                        if (cmd != "-") {
                            for (int i = 0; i < cmd.Length; i++) {
                                boardStateMask |= cmd[i] switch {
                                    'K' => ChessBoard.BoardStateMask.WRCastling,
                                    'Q' => ChessBoard.BoardStateMask.WLCastling,
                                    'k' => ChessBoard.BoardStateMask.BRCastling,
                                    'q' => ChessBoard.BoardStateMask.BLCastling,
                                     _  => 0
                                };
                            }
                        }
                        cmd = cmds[3];
                        if (cmd == "-") {
                            enPassantPos = 0;
                        }
                    }
                }
            }
            return retVal;
        }


        /// <summary>
        /// Apply a SAN move to the board
        /// </summary>
        /// <param name="pgnGame"> PGN game</param>
        /// <param name="sanTxt">  SAN move</param>
        /// <param name="move">    Converted move</param>
        /// <returns>
        /// true if succeed, false if failed
        /// </returns>
        public bool ApplySanMoveToBoard(PgnGame pgnGame, string? sanTxt, out MoveExt move) {
            bool retVal;
            int  truncatedCount = 0;

            move = new MoveExt(ChessBoard.PieceType.None,
                               startPos: 0,
                               endPos:   0,
                               Move.MoveType.Normal,
                               comment: "",
                               permutationCount: -1,
                               searchDepth: -1,
                               cacheHit: 0,
                               nagCode: 0);
            if (!string.IsNullOrEmpty(sanTxt)) {
                try {
                    CnvSanMoveToPosMove(pgnGame, 
                                        m_chessBoard.CurrentPlayer,
                                        sanTxt,
                                        out short pos,
                                        ref truncatedCount,
                                        ref move);
                    retVal = (truncatedCount == 0);
                } catch(PgnParserException) {
                    retVal = false;
                }
            } else {
                retVal = false;
            }
            return retVal;
        }
    } // Class PgnParser
} // Namespace
