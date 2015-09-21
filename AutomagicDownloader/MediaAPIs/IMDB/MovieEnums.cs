using System.ComponentModel;

namespace MediaAPIs.IMDB
{
    public enum MovieView
    {
        Compact,
        Grid,
        Detail
    }

    public enum MediaType
    {
        [Description("Documentary")]
        Documentary,
        [Description("Feature Film")]
        Feature,
        [Description("Mini Series")]
        MiniSeries,
        [Description("Short Film")]
        ShortFilm,
        [Description("TV Series")]
        TVSeries,
        [Description("TV Episode")]
        TVEpisode,
        [Description("TV Movie")]
        TVMovie,
        [Description("Video")]
        Video
    }
}