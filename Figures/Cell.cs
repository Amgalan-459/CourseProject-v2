using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestChessWPF;

namespace Figures
{
    public class Cell : NotifyPropertyChanged
    {
        private IFigure _figure;
        private bool _active;

        public IFigure Figure
        {
            get => _figure;
            set
            {
                _figure = value;
                OnPropertyChanged(); // сообщить интерфейсу, что значение поменялось, чтобы интефейс перерисовался в этом месте
            }
        }
        public bool Active // это будет показывать, что ячейка выделена пользователем
        {
            get => _active;
            set
            {
                _active = value;
                OnPropertyChanged();
            }
        }
    }
}
