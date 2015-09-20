using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace MediaAPIs.IMDB
{
    public class IMDBClient : MediaClient
    {
        private const string WatchListUrl = "http://www.imdb.com/user/{0}/watchlist";

        public IMDBClient(HttpClientHandler newHandler = null)
        {
            Handler = newHandler ?? new HttpClientHandler();
            Client = new HttpClient(Handler);
        }

        public List<MediaItem> GetWatchList(string user)
        {
            var watchListHTML = Client.GetStringAsync(string.Format(WatchListUrl, user)).Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(watchListHTML);
            var unparsedMovies = doc.DocumentNode.SelectNodes("//div[@class=\"lister-item-content\"]");
            if (unparsedMovies.Count == 0)
            {
                throw new ArgumentException("User either doesn't have anything in their watch list or their watchlist isn't public");
            }
            var movies = new List<MediaItem>();
            foreach (var unparsedMovieNode in unparsedMovies)
            {
                var movie = new Movie();
                var movieNameIdNode = unparsedMovieNode.SelectSingleNode("h3/a[@href]");
                movie.Id = Regex.Match(movieNameIdNode.Attributes["href"].Value, "(tt[0-9]+)").Value;
                movie.Title = movieNameIdNode.InnerText;
                var ratingNode = unparsedMovieNode.SelectSingleNode("p/span[@class=\"certificate\"]");
                if (ratingNode != null)
                {
                    movie.Classification = ClassificationHelper.ParseClassification(ratingNode.InnerText);
                }
                movies.Add(movie);
            }
            return movies;
        }
    }
}
