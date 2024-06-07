using System.Windows.Controls;
using SrcChess2.Core;

namespace SrcChess2 {
    public abstract class PieceSet {

        protected enum ChessPiece {
            None         = -1,
            Black_Pawn   = 0,
            Black_Rook   = 1,
            Black_Bishop = 2,
            Black_Knight = 3,
            Black_Queen  = 4,
            Black_King   = 5,
            White_Pawn   = 6,
            White_Rook   = 7,
            White_Bishop = 8,
            White_Knight = 9,
            White_Queen  = 10,
            White_King   = 11
        };


        public string Name { get; private set; }

        protected PieceSet(string name) => Name = name;

        private static ChessPiece GetChessPieceFromPiece(ChessBoard.PieceType pieceType)
            => pieceType switch {
                ChessBoard.PieceType.Pawn   | ChessBoard.PieceType.White => ChessPiece.White_Pawn,
                ChessBoard.PieceType.Knight | ChessBoard.PieceType.White => ChessPiece.White_Knight,
                ChessBoard.PieceType.Bishop | ChessBoard.PieceType.White => ChessPiece.White_Bishop,
                ChessBoard.PieceType.Rook   | ChessBoard.PieceType.White => ChessPiece.White_Rook,
                ChessBoard.PieceType.Queen  | ChessBoard.PieceType.White => ChessPiece.White_Queen,
                ChessBoard.PieceType.King   | ChessBoard.PieceType.White => ChessPiece.White_King,
                ChessBoard.PieceType.Pawn   | ChessBoard.PieceType.Black => ChessPiece.Black_Pawn,
                ChessBoard.PieceType.Knight | ChessBoard.PieceType.Black => ChessPiece.Black_Knight,
                ChessBoard.PieceType.Bishop | ChessBoard.PieceType.Black => ChessPiece.Black_Bishop,
                ChessBoard.PieceType.Rook   | ChessBoard.PieceType.Black => ChessPiece.Black_Rook,
                ChessBoard.PieceType.Queen  | ChessBoard.PieceType.Black => ChessPiece.Black_Queen,
                ChessBoard.PieceType.King   | ChessBoard.PieceType.Black => ChessPiece.Black_King,
                _                                                        => ChessPiece.None,
            };


        protected abstract UserControl LoadPiece(ChessPiece chessPiece);

        public UserControl? this[ChessBoard.PieceType pieceType] {
            get {
                UserControl? retVal;
                ChessPiece   chessPiece;

                chessPiece  = GetChessPieceFromPiece(pieceType);
                retVal      = chessPiece == ChessPiece.None ? null : LoadPiece(chessPiece);
                return retVal;
            }
        }
    }
}
