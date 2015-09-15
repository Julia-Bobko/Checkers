using System;
using System.Collections.Generic;
using System.Text;

namespace Checkers.Entities
{
    public class Gamer
    {
        public long SocialId { get; set; }
        public string Authentication { get; set; }
        public string ImageSource { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string City { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
        public string HashPassword { get; set; }
    }
}
