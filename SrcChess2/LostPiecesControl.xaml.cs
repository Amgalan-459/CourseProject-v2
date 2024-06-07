using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SrcChess2.Core;

namespace SrcChess2 {
    public partial class LostPiecesControl : UserControl {
        private readonly Border[]               m_borders;
        private readonly ChessBoard.PieceType[] m_pieceTypes;
        private ChessBoardControl?              m_chessBoardCtl;
        private PieceSet?                       m_pieceSet;
        private bool                            m_isDesignMode;
        private int                             m_selectedPiece;
        public bool                             Color { get; set; }
        
        public LostPiecesControl() {
            Border   border;

            InitializeComponent();
            m_selectedPiece    = -1;
            m_borders         = new Border[16];
            m_pieceTypes          = new ChessBoard.PieceType[16];
            for (int i = 0; i < 16; i++) {
                border = new Border {
                    Margin          = new Thickness(1),
                    BorderThickness = new Thickness(1),
                    BorderBrush     = Background
                };
                m_borders[i]       = border;
                m_pieceTypes[i]    = ChessBoard.PieceType.None;
                CellContainer.Children.Add(border);
            }
        }

        private ChessBoard.PieceType[] EnumPiece() {
            ChessBoard.PieceType[] pieceTypes;
            ChessBoard.PieceType[] possiblePieceTypes;
            ChessBoard.PieceType   pieceType;
            int                    eatedPieces;
            int                    pos;
            
            pieceTypes           = new ChessBoard.PieceType[16];
            for (int i = 0; i < 16; i++) {
                pieceTypes[i]   = ChessBoard.PieceType.None;
            }
            possiblePieceTypes  = [ ChessBoard.PieceType.King,
                                    ChessBoard.PieceType.Queen,
                                    ChessBoard.PieceType.Rook,
                                    ChessBoard.PieceType.Bishop,
                                    ChessBoard.PieceType.Knight,
                                    ChessBoard.PieceType.Pawn ];
            pos = 0;
            if (m_isDesignMode) {
                pos++;
            }
            foreach (ChessBoard.PieceType possiblePieceType in possiblePieceTypes) {
                if (m_isDesignMode) {
                    pieceType         = possiblePieceType;
                    pieceTypes[pos++] = pieceType;
                    pieceType        |= ChessBoard.PieceType.Black;
                    pieceTypes[pos++] = pieceType;
                } else {                    
                    pieceType = possiblePieceType;
                    if (Color) {
                        pieceType |= ChessBoard.PieceType.Black;
                    }
                    eatedPieces = m_chessBoardCtl!.ChessBoard.GetEatedPieceCount(pieceType);
                    for (int i = 0; i < eatedPieces; i++) {
                        pieceTypes[pos++] = pieceType;
                    }
                }
            }
            return pieceTypes;
        }

        private static Size MakeSquare(Size size) {
            double  minSize;

            minSize = (size.Width < size.Height) ? size.Width : size.Height;
            size    = new Size(minSize, minSize);
            return size;
        }
        
        protected override Size MeasureOverride(Size constraint) {
            constraint = MakeSquare(constraint);
 	        return base.MeasureOverride(constraint);
        }

        private void SetPieceControl(int pos, ChessBoard.PieceType pieceType) {
            Border   border;
            Control? controlPiece;
            Label    label;

            border       = m_borders[pos];
            controlPiece = m_pieceSet![pieceType];
            if (controlPiece != null) {
                controlPiece.Margin = new Thickness(1);
            }
            m_pieceTypes[pos] = pieceType;
            if (controlPiece == null) { // && m_bDesignMode) {
                label = new Label {
                    Content     = " ",
                    FontSize    = 0.1
                };
                controlPiece                     = label;
                controlPiece.HorizontalAlignment = HorizontalAlignment.Stretch;
                controlPiece.VerticalAlignment   = VerticalAlignment.Stretch;
            }
            border.Child = controlPiece;
        }

        private void RefreshCell(ChessBoard.PieceType[] newPieces, int pos, bool isFullRefresh) {
            ChessBoard.PieceType pieceType;

            pieceType = newPieces[pos];
            if (pieceType != m_pieceTypes[pos] || isFullRefresh) {
                SetPieceControl(pos, pieceType);
            }
        }

        private void Refresh(bool isFullRefresh) {
            ChessBoard             chessBoard;
            ChessBoard.PieceType[] newPieceTypes;

            if (m_chessBoardCtl != null && m_chessBoardCtl.ChessBoard != null && m_pieceSet != null) {
                newPieceTypes = EnumPiece();
                chessBoard    = m_chessBoardCtl.ChessBoard;
                if (chessBoard != null) {
                    for (int pos = 0; pos < 16; pos++) {
                        RefreshCell(newPieceTypes, pos, isFullRefresh);
                    }
                }
            }
        }
        public void Refresh() => Refresh(isFullRefresh: false);
        
        public ChessBoardControl? ChessBoardControl {
            get => m_chessBoardCtl;
            set {
                if (m_chessBoardCtl != value) {
                    m_chessBoardCtl = value;
                    Refresh(isFullRefresh: false);
                }
            }
        }

        public PieceSet? PieceSet {
            get => m_pieceSet;
            set {
                if (m_pieceSet != value) {
                    m_pieceSet = value;
                    Refresh(isFullRefresh: true);
                }
            }
        }

        public int SelectedIndex {
            get => m_selectedPiece;
            set {
                if (m_selectedPiece != value) {
                    if (value >= 0 && value < 13) {
                        if (m_selectedPiece != -1) {
                            m_borders[m_selectedPiece].BorderBrush = Background;
                        }
                        m_selectedPiece = value;
                        if (m_selectedPiece != -1) {
                            m_borders[m_selectedPiece].BorderBrush = MainBorder.BorderBrush;
                        }
                    }
                }
            }
        }

        public ChessBoard.PieceType SelectedPiece {
            get {
                ChessBoard.PieceType retVal = ChessBoard.PieceType.None;
                int                  selectedIndex;
                
                selectedIndex = SelectedIndex;
                if (selectedIndex > 0 && selectedIndex < 13) {
                    selectedIndex--;
                    if ((selectedIndex & 1) != 0) {
                        retVal |= ChessBoard.PieceType.Black;
                    }
                    selectedIndex >>= 1;
                    switch(selectedIndex) {
                    case 0:
                        retVal |= ChessBoard.PieceType.King;
                        break;
                    case 1:
                        retVal |= ChessBoard.PieceType.Queen;
                        break;
                    case 2:
                        retVal |= ChessBoard.PieceType.Rook;
                        break;
                    case 3:
                        retVal |= ChessBoard.PieceType.Bishop;
                        break;
                    case 4:
                        retVal |= ChessBoard.PieceType.Knight;
                        break;
                    case 5:
                        retVal |= ChessBoard.PieceType.Pawn;
                        break;
                    default:
                        retVal = ChessBoard.PieceType.None;
                        break;
                    }
                }
                return retVal;
            }
        }

        public bool BoardDesignMode {
            get => m_isDesignMode;
            set {
                if (m_isDesignMode != value) {
                    SelectedIndex = -1;
                    m_isDesignMode = value;
                    Refresh(isFullRefresh: false);
                    if (m_isDesignMode) {
                        SelectedIndex   = 0;
                    }
                }
            }
        }
        protected override void OnMouseUp(MouseButtonEventArgs e) {
            Point pt;
            int   rowPos;
            int   colPos;
            int   pos;

            base.OnMouseUp(e);
            if (m_isDesignMode) {
                pt     = e.GetPosition(this);
                rowPos = (int)(pt.Y * 4 / ActualHeight);
                colPos = (int)(pt.X * 4 / ActualWidth);
                if (rowPos >= 0 && rowPos < 4 && colPos >= 0 && colPos < 4) {
                    pos            = (rowPos << 2) + colPos;
                    SelectedIndex  = (pos < 13) ? pos : 0;
                }
            }
        }
    } // Class LostPiecesControl
} // Namespace
