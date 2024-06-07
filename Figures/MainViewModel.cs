using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TestChessWPF;

namespace Figures
{
    public class MainViewModel : NotifyPropertyChanged
    {
        private Board _board = new Board();
        private ICommand _newGameCommand;
        private ICommand _clearCommand;
        private ICommand _cellCommand;

        public IEnumerable<char> Numbers => "87654321";
        public IEnumerable<char> Letters => "ABCDEFGH";

        public Board Board
        {
            get => _board;
            set
            {
                _board = value;
                OnPropertyChanged();
            }
        }

        public ICommand NewGameCommand => _newGameCommand ??= new RelayCommand(parameter =>
        {
            SetupBoard();
        });

        public ICommand ClearCommand => _clearCommand ??= new RelayCommand(parameter =>
        {
            Board = new Board();
        });

        public ICommand CellCommand => _cellCommand ??= new RelayCommand(parameter =>
        {
            Cell cell = (Cell)parameter;
            Cell activeCell = Board.FirstOrDefault(x => x.Active);
            //if (cell.State != State.Empty)
            if (cell.Figure !=  null)
            {
                if (!cell.Active && activeCell != null)
                    activeCell.Active = false;
                cell.Active = !cell.Active;
            }
            else if (activeCell != null)
            {
                activeCell.Active = false;
                cell.Figure = activeCell.Figure;
                activeCell.Figure = null;
            }
        }, parameter => parameter is Cell cell && (Board.Any(x => x.Active) || cell.Figure != null));

        private void SetupBoard()
        {
            Board board = new Board();
            board[0, 0] = new Rook(false, true);
            board[0, 1] = new Horse(false, true);
            board[0, 2] = new Elephant(false, true);
            board[0, 3] = new Queen(false, true);
            board[0, 4] = new King(false, true);
            board[0, 5] = new Elephant(false, true);
            board[0, 6] = new Horse(false, true);
            board[0, 7] = new Rook(false, true);
            for (int i = 0; i < 8; i++)
            {
                board[1, i] = new Pawn(false, true);
                board[6, i] = new Pawn(true, true);
            }
            board[7, 0] = new Rook(true, true);
            board[7, 1] = new Horse(true, true);
            board[7, 2] = new Elephant(true, true);
            board[7, 3] = new Queen(true, true);
            board[7, 4] = new King(true, true);
            board[7, 5] = new Elephant(true, true);
            board[7, 6] = new Horse(true, true);
            board[7, 7] = new Rook(true, true);
            Board = board;
        }

        public MainViewModel()
        {
        }
    }
}
