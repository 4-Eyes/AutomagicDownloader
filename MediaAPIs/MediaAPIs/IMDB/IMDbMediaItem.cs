using System;
using System.Collections.Generic;

namespace MediaAPIs.IMDb
{
    public class IMDbMediaItem : MediaItem
    {
        public IMDbMediaItem()
        {
            Keywords = new List<KeyWord>();
            Cast = new List<Credit>();
            Producers = new List<Credit>();
            Composers = new List<Credit>();
            Directors = new List<Credit>();
            Writers = new List<Credit>();
            OtherCrew = new List<Credit>();
            OtherTitles = new List<string>();
        }

        /// <summary>
        ///     The Release date of the media item.
        /// </summary>
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        ///     The runtime of the media itme. Note if it is a TV show this is currently not calculated.
        /// </summary>
        public TimeSpan RunTime { get; set; }

        /// <summary>
        ///     The type of the meda. See MediaType enum for all options.
        /// </summary>
        public MediaType Type { get; set; }

        /// <summary>
        ///     The name of the episode the media is. This will only be set if the Media Type is TV Episode otherwise
        ///     it will be null.
        /// </summary>
        public string EpisodeName { get; set; }

        public List<Credit> Directors { get; private set; }
        public List<Credit> Writers { get; private set; }
        public List<Credit> Cast { get; private set; }
        public List<Credit> Producers { get; private set; }
        public List<Credit> Composers { get; private set; }
        public List<Credit> OtherCrew { get; private set; }
        public List<string> OtherTitles { get; private set; }
        public int MetacriticScore { get; set; }
        public string ShortSummary { get; set; }
        public List<KeyWord> Keywords { get; private set; }
    }
}