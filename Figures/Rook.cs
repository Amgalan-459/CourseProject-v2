﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Figures
{
    public class Rook : IFigure
    {
        public bool IsWhite { get; set; }
        public bool IsActive { get; set; }

        public bool IsValidMove()
        {
            return true;
        }

        public Rook(bool isWhite, bool isActive)
        {
            IsWhite = isWhite;
            IsActive = isActive;
        }
    }
}