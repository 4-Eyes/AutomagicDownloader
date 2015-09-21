using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

namespace MediaAPIs.IMDB
{
    public class IMDBClient : MediaClient
    {

        private const string WatchListUrl = "http://www.imdb.com/user/{0}/watchlist";
        private const string TitleUrl = "http://www.imdb.com/title/{0}";
        private const string RatingsListUrl = "http://www.imdb.com/user/{0}/ratings";
        private NameValueCollection DefaultRatingsQueryString = new NameValueCollection()
        {
            {"view", "detail" }, //Other options include simple, ...
            {"sort", "title:asc" }, 
            {"defaults", "1" },
            {"start", "1" } //Increment this in 100's till no movies can be parsed
        };

        public IMDBClient(HttpClientHandler newHandler = null)
        {
            Handler = newHandler ?? new HttpClientHandler();
            Client = new HttpClient(Handler);
        }

        public async Task<List<MediaItem>> GetPublicRatingsAsync(string user)
        {
            var userRatingsHTML = await Client.GetStringAsync(string.Format(RatingsListUrl, user) + ToQueryString(DefaultRatingsQueryString));
            var doc = new HtmlDocument();
            doc.LoadHtml(userRatingsHTML);
            var unparsedMovies = doc.DocumentNode.SelectNodes("//tr[@data-item-id]");

            return unparsedMovies.Select(node => ParseRatingsListMovieHTML(node, MovieView.Compact)).ToList();
        }

        public async Task<List<MediaItem>> GetPublicWatchListAsync(string user)
        {
            var watchListHTML = await Client.GetStringAsync(string.Format(WatchListUrl, user));
            var doc = new HtmlDocument();
            doc.LoadHtml(watchListHTML);
            var unparsedMovies = doc.DocumentNode.SelectNodes("//div[@class=\"lister-item-content\"]");
            if (unparsedMovies.Count == 0)
            {
                throw new ArgumentException("User either doesn't have anything in their watch list or their watchlist isn't public");
            }
            return unparsedMovies.Select(ParseWatchListMovieHTML).ToList();
        }

        private static MediaItem ParseRatingsListMovieHTML(HtmlNode movieDetailNode, MovieView view)
        {
            var movie = new Movie();
            var titleNode = movieDetailNode.SelectSingleNode("td[@class=\"title\"]/a[@href]");
            if (titleNode != null)
            {
                movie.Id = Regex.Match(titleNode.Attributes["href"].Value, "(tt[0-9]+)").Value;
                movie.Title = titleNode.InnerText;
            }
            var yearNode = movieDetailNode.SelectSingleNode("td[@class=\"year\"]");
            if (yearNode != null)
            {
                movie.ReleaseDate = new DateTime(int.Parse(yearNode.InnerText), 1, 1);
            }
            return movie;
        }

        private static MediaItem ParseWatchListMovieHTML(HtmlNode movieDetailNode)
        {
            var movie = new Movie();
            var movieNameIdNode = movieDetailNode.SelectSingleNode("h3/a[@href]");
            movie.Id = Regex.Match(movieNameIdNode.Attributes["href"].Value, "(tt[0-9]+)").Value;
            movie.Title = movieNameIdNode.InnerText;
            var classificationNode = movieDetailNode.SelectSingleNode("p/span[@class=\"certificate\"]");
            if (classificationNode != null)
            {
                movie.Classification = ClassificationHelper.ParseClassification(classificationNode.InnerText);
            }
            var runtimeNode = movieDetailNode.SelectSingleNode("p/span[@class=\"runtime\"]");
            if (runtimeNode != null)
            {
                movie.RunTime = TimeSpan.FromMinutes(double.Parse(runtimeNode.InnerText.Replace("min", "")));
            }
            var genresNode = movieDetailNode.SelectSingleNode("p/span[@class=\"genre\"]");
            if (genresNode != null)
            {
                movie.Genres = genresNode.InnerText.Split(',').Select(s => s.Trim()).ToList();
            }
            var ratingNode =
                movieDetailNode.SelectSingleNode("div/div[@class=\"inline-block ratings-imdb-rating\"]/strong");
            if (ratingNode != null)
            {
                movie.Rating = double.Parse(ratingNode.InnerText);
            }
            var yearNode = movieDetailNode.SelectSingleNode("h3/span[@class=\"lister-item-year text-muted unbold\"]");
            if (yearNode != null)
            {
                var yearString = Regex.Match(yearNode.InnerText, "([0-9]+)").Value;
                movie.ReleaseDate = !string.IsNullOrWhiteSpace(yearString) ? new DateTime(int.Parse(yearString), 1, 1) : DateTime.MinValue;
            }
            var synopsisNode = movieDetailNode.SelectSingleNode("p[@class=\"\"]");
            if (synopsisNode != null)
            {
                movie.Synopsis = synopsisNode.InnerText.Trim();
            }
            var posterURLNode =
                movieDetailNode.SelectSingleNode("../div[@class=\"lister-item-image ribbonize\"]/a/img[@src]");
            if (posterURLNode != null)
            {
                movie.PosterURL = posterURLNode.Attributes["src"].Value;
            }
            return movie;
        }

        public MediaItem GetMovie(string imdbId)
        {
            var pageHtml = Client.GetStringAsync(string.Format(TitleUrl, imdbId)).Result;
            var doc = new HtmlDocument();
            doc.LoadHtml(pageHtml);
            return new Movie();
        }

        private static string ToQueryString(NameValueCollection nvc)
        {
            var array = (from key in nvc.AllKeys
                         from value in nvc.GetValues(key)
                         select $"{HttpUtility.UrlEncode(key)}={HttpUtility.UrlEncode(value)}")
                .ToArray();
            return "?" + string.Join("&", array);
        }
    }
}
