using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Musicaly {
    internal class Track {
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
