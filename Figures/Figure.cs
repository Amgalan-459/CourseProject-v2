using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Figures
{
    public interface IFigure
    {
        public bool IsWhite { get; set; }
        public bool IsActive { get;set; }

        public bool IsValidMove();
    }
}
