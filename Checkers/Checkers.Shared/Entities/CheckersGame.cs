using System;
using System.Collections.Generic;
using System.Text;

namespace Checkers.Entities
{
    public class CheckersGame
    {
        public int IdGame { get; set; }
        public string Login { get; set; }
        public int Rating { get; set; }
        public int IdFirstGamer { get; set; }
    }
}
