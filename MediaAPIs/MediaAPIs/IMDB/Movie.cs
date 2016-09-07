using System;
using System.Collections.Generic;

namespace MediaAPIs.IMDB
{
    public class Movie : MediaItem
    {
        /// <summary>
        /// The Release date of the media item.
        /// </summary>
        public DateTime ReleaseDate { get; set; }
        /// <summary>
        /// The runtime of the media itme. Note if it is a TV show this is currently not calculated.
        /// </summary>
        public TimeSpan RunTime { get; set; }
        /// <summary>
        /// The type of the meda. See MediaType enum for all options.
        /// </summary>
        public MediaType Type { get; set; }
        /// <summary>
        /// The name of the episode the media is. This will only be set if the Media Type is TV Episode otherwise
        /// it will be null.
        /// </summary>
        public string EpisodeName { get; set; }
    }
}