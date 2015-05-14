using System;
using System.Collections.Generic;
using System.Text;

namespace Checkers.Entities
{
    public class UpdateStatus
    {
        public int IdGame { set; get; }
        public int IdGamer { set; get; }
        public string XmlCurrentStatus { set; get; }
        public string LastMovedCheckers { get; set; }
        public string LastDeletedCheckers { get; set; }
    }
}
