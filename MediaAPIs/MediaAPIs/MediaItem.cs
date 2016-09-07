using System;
using System.Collections.Generic;

namespace MediaAPIs
{
    public class MediaItem
    {
        public MediaItem()
        {
            Genres = new List<string>();
        }

        public string Id { get; set; }
        public string Title { get; set; }
        public string Synopsis { get; set; }
        public double Rating { get; set; }
        public int NumberOfVotes { get; set; }
        public double UserRating { get; set; }
        public Enum Classification { get; set; }
        public List<string> Genres { get; private set; }
        public string PosterURL { get; set; }
    }
}