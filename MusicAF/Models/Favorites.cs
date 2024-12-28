using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicAF.Models
{
    class Favorites
    {
        public string UserId { get; set; }
        public List<string> TrackIds { get; set; } = new List<string>();
    }
}
