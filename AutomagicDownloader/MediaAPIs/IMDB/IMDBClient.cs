using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
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
                var classificationNode = unparsedMovieNode.SelectSingleNode("p/span[@class=\"certificate\"]");
                if (classificationNode != null)
                {
                    movie.Classification = ClassificationHelper.ParseClassification(classificationNode.InnerText);
                }
                var runtimeNode = unparsedMovieNode.SelectSingleNode("p/span[@class=\"runtime\"]");
                if (runtimeNode != null)
                {
                    movie.RunTime = TimeSpan.FromMinutes(double.Parse(runtimeNode.InnerText.Replace("min", "")));
                }
                var genresNode = unparsedMovieNode.SelectSingleNode("p/span[@class=\"genre\"]");
                if (genresNode != null)
                {
                    movie.Genres = genresNode.InnerText.Split(',').Select(s => s.Trim()).ToList();
                }
                var ratingNode =
                    unparsedMovieNode.SelectSingleNode("div/div[@class=\"inline-block ratings-imdb-rating\"]/strong");
                if (ratingNode != null)
                {
                    movie.Rating = double.Parse(ratingNode.InnerText);
                }
                var yearNode = unparsedMovieNode.SelectSingleNode("h3/span[@class=\"lister-item-year text-muted unbold\"]");
                if (yearNode != null)
                {
                    var yearString = Regex.Match(yearNode.InnerText, "([0-9]+)").Value;
                    movie.ReleaseDate = !string.IsNullOrWhiteSpace(yearString) ? new DateTime(int.Parse(yearString), 1, 1) : DateTime.MinValue;
                }
                var synopsisNode = unparsedMovieNode.SelectSingleNode("p[@class=\"\"]");
                if (synopsisNode != null)
                {
                    movie.Synopsis = synopsisNode.InnerText.Trim();
                }
                var posterURLNode =
                    unparsedMovieNode.SelectSingleNode("../div[@class=\"lister-item-image ribbonize\"]/a/img[@src]");
                if (posterURLNode != null)
                {
                    movie.PosterURL = posterURLNode.Attributes["src"].Value;
                }
                movies.Add(movie);
            }
            return movies;
        }
    }
}
