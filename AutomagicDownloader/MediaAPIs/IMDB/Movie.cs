using System;
using System.Collections.Generic;

namespace MediaAPIs.IMDB
{
    public class Movie : MediaItem
    {
        public DateTime ReleaseDate { get; set; }
        public TimeSpan RunTime { get; set; }
        public MediaType Type { get; set; }
    }
}