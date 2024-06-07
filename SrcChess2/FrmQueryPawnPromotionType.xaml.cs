using System.Windows;
using SrcChess2.Core;

namespace SrcChess2 {
    public partial class FrmQueryPawnPromotionType : Window {
        private readonly ChessBoard.ValidPawnPromotion  m_validPawnPromotion;

        public FrmQueryPawnPromotionType() => InitializeComponent();

        public FrmQueryPawnPromotionType(ChessBoard.ValidPawnPromotion validPawnPromotion) : this() {
            m_validPawnPromotion        = validPawnPromotion;
            radioButtonQueen.IsEnabled  = ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Queen)  != ChessBoard.ValidPawnPromotion.None);
            radioButtonRook.IsEnabled   = ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Rook)   != ChessBoard.ValidPawnPromotion.None);
            radioButtonBishop.IsEnabled = ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Bishop) != ChessBoard.ValidPawnPromotion.None);
            radioButtonKnight.IsEnabled = ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Knight) != ChessBoard.ValidPawnPromotion.None);
            if ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Queen)  != ChessBoard.ValidPawnPromotion.None) {
                radioButtonQueen.IsChecked  = true;
            } else if ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Rook)   != ChessBoard.ValidPawnPromotion.None) {
                radioButtonRook.IsChecked   = true;
            } else if ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Bishop) != ChessBoard.ValidPawnPromotion.None) {
                radioButtonBishop.IsChecked = true;
            } else if ((m_validPawnPromotion & ChessBoard.ValidPawnPromotion.Knight) != ChessBoard.ValidPawnPromotion.None) {
                radioButtonKnight.IsChecked = true;
            }
        }

        public Move.MoveType PromotionType {
            get {
                Move.MoveType retVal;
                
                if (radioButtonRook.IsChecked == true) {
                    retVal = Move.MoveType.PawnPromotionToRook;
                } else if (radioButtonBishop.IsChecked == true) {
                    retVal = Move.MoveType.PawnPromotionToBishop;
                } else if (radioButtonKnight.IsChecked == true) {
                    retVal = Move.MoveType.PawnPromotionToKnight;
                } else {
                    retVal = Move.MoveType.PawnPromotionToQueen;
                }
                return retVal;
            }
        }

        private void ButOk_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }
    }
}
