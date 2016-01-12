using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

namespace MediaAPIs.IMDB
{
    public class IMDBClient : MediaClient
    {
        /// <summary>
        /// The url that is used for getting a public watchlist.
        /// </summary>
        private const string WatchListUrl = "http://www.imdb.com/user/{0}/watchlist";
        /// <summary>
        /// The url that is used to any imdb title (i.e. tt2396224)
        /// </summary>
        private const string TitleUrl = "http://www.imdb.com/title/{0}";
        /// <summary>
        /// The url used to get user ratings (as long as the list is public)
        /// </summary>
        private const string RatingsListUrl = "http://www.imdb.com/user/{0}/ratings";
        /// <summary>
        /// The url for a users main page.
        /// </summary>
        private const string UserUrl = "http://www.imdb.com/user/{0}";

        public IMDBClient(HttpClientHandler newHandler = null)
        {
            Handler = newHandler ?? new HttpClientHandler();
            Client = new HttpClient(Handler);
        }

        public async Task<List<MediaItem>> GetPublicRatingsAsync(string user, MovieView view = MovieView.Compact)
        {
            var ratedMovies = new List<MediaItem>();

            var tasks = new List<Task>();
            // Get the number of ratings
            var userPageHTML = await Client.GetStringAsync(string.Format(UserUrl, user));
            var doc = new HtmlDocument();
            doc.LoadHtml(userPageHTML);
            var numberRatingsNode = doc.DocumentNode.SelectSingleNode($"//a[@href='/user/{user}/ratings']");
            if (numberRatingsNode == null) throw new Exception("User has no ratings or their ratings aren't public");
            var totalRatingsMatch = Regex.Match(numberRatingsNode.InnerText, "(?<num>[0-9,]+)");
            var totalRatings = int.Parse(totalRatingsMatch.Groups["num"].Value.Replace(",", ""));

            var start = 0;
            do
            {
                var headers = new NameValueCollection()
                {
                        {"view", view.ToString().ToLower() }, //Other options include compact, detail and grid
                        {"sort", "title:asc" }, //Other options rate_date:(desc|asc)...
                        {"defaults", "1" },
                        {"start", start.ToString() } //Increment this in 100's till no movies can be parsed
                };
                tasks.Add(Task.Run(async () =>
                {
                    var movies = await ParseRatingPage(user, view, headers);
                    if (movies == null) return;
                    ratedMovies.AddRange(movies); }));

                start += view.GetInterval();
            } while (totalRatings > start);

            while (tasks.Count > 0)
            {
                var finishedTask = await Task.WhenAny(tasks);

                tasks.Remove(finishedTask);

                await finishedTask;
            }

            return ratedMovies;
        }

        private async Task<IEnumerable<MediaItem>> ParseRatingPage(string user, MovieView view, NameValueCollection headers)
        {
            var userRatingsHTML = await Client.GetStringAsync(string.Format(RatingsListUrl, user) + ToQueryString(headers));
            var doc = new HtmlDocument();
            doc.LoadHtml(userRatingsHTML);
            var unparsedMovies = doc.DocumentNode.SelectNodes(view.GetXPathQuery());
            if (unparsedMovies == null || unparsedMovies.Count == 0) return null;
            return unparsedMovies.Select(unparsedMovie => ParseRatingsListMovieHTML(unparsedMovie, view)).Where(parsedMoive => parsedMoive != null);
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
            switch (view)
            {
                case MovieView.Compact:
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
                    var typeNode = movieDetailNode.SelectSingleNode("td[@class=\"title_type\"]");
                    if (typeNode != null)
                    {
                        foreach (var value in Enum.GetValues(typeof(MediaType)).Cast<object>().Where(value => value.ToString() == typeNode.InnerText.Replace("-", "").Replace(" ", "")))
                        {
                            movie.Type = (MediaType) value;
                            break;
                        }
                    }
                    var raterRatingNode = movieDetailNode.SelectSingleNode("td[@class=\"rater_ratings\"]");
                    if (raterRatingNode != null)
                    {
                        movie.UserRating = double.Parse(raterRatingNode.InnerText);
                    }
                    var userRatingNode = movieDetailNode.SelectSingleNode("td[@class=\"user_rating\"]");
                    if (userRatingNode != null)
                    {
                        movie.Rating = double.Parse(userRatingNode.InnerText);
                    }
                    var numVotesNode = movieDetailNode.SelectSingleNode("td[@class=\"num_votes\"]");
                    if (numVotesNode != null)
                    {
                        movie.NumberOfVotes = int.Parse(numVotesNode.InnerText.Replace(",", ""));
                    }
                    break;
                case MovieView.Detail:
                    var infoNode = movieDetailNode.SelectSingleNode("div[@class=\"info\"]");
                    var titleYearTypeNode = infoNode.SelectSingleNode("b");
                    if (titleYearTypeNode != null)
                    {
                        var test = titleYearTypeNode.SelectSingleNode("a");
                        if (test == null) return null;
                        movie.Title = test.InnerText;
                        var match = Regex.Match(titleYearTypeNode.SelectSingleNode("span").InnerText,
                            "\\((?<year>[0-9]+).(?<type>[a-zA-z]+|)");
                        if (match.Success)
                        {
                            movie.ReleaseDate = new DateTime(int.Parse(match.Groups["year"].Value), 1, 1);
                            object type = null;
                            if (string.IsNullOrEmpty(match.Groups["type"].Value))
                            {
                                type = MediaType.Feature;
                            }
                            else
                            {
                                foreach (var value in Enum.GetValues(typeof (MediaType)).Cast<object>().Where(value => value.ToString() == match.Groups["type"].Value.Replace("-", "").Replace(" ", "")))
                                {
                                    type = (MediaType) value;
                                    break;
                                }
                            }
                            if (type != null) {
                                if ((MediaType) type == MediaType.TVSeries)
                                {
                                    //This part checks to see if it is a TV Episode
                                    var episodeNode = infoNode.SelectSingleNode("div[@class=\"episode\"]");
                                    if (episodeNode != null)
                                    {
                                        type = MediaType.TVEpisode;
                                        movie.EpisodeName = episodeNode.SelectSingleNode("a").InnerText;
                                    }
                                }
                                movie.Type = (MediaType) type;
                            }

                        }
                    }
                    var idRatingsNode = infoNode.SelectSingleNode("div[@class=\"rating rating-list\"]");
                    if (idRatingsNode != null)
                    {
                        var match = Regex.Match(idRatingsNode.Attributes["id"].Value,
                            "(?<id>tt[0-9]+)\\|[^\\|]+\\|(?<userRating>[0-9]+)\\|(?<imdbRating>[0-9\\.]+)");
                        if (match.Success)
                        {
                            movie.Id = match.Groups["id"].Value;
                            movie.UserRating = double.Parse(match.Groups["userRating"].Value);
                            movie.Rating = double.Parse(match.Groups["imdbRating"].Value);
                        }
                    }
                    var synopsisRuntimeNode = infoNode.SelectSingleNode("div[@class=\"item_description\"]");
                    if (synopsisRuntimeNode != null)
                    {
                        movie.Synopsis = synopsisRuntimeNode.InnerText;
                        var runtimeNode = synopsisRuntimeNode.SelectSingleNode("span");
                        if (runtimeNode != null)
                        {
                            var runtime =
                                Regex.Match(runtimeNode.InnerText, "(?<runtime>[0-9]+)").Groups["runtime"].Value;
                            movie.RunTime = TimeSpan.FromMinutes(int.Parse(runtime));
                        }
                    }
                    //Todo you can still get the poster URL from scraping this version.
                    break;
                case MovieView.Grid:
                    throw new NotImplementedException("Have not implemented Grid parsing yet");
                default:
                    throw new ArgumentOutOfRangeException(nameof(view), view, "No implementation for this MovieView");
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
            var ratingNode = movieDetailNode.SelectSingleNode("div/div[@class=\"inline-block ratings-imdb-rating\"]/strong");
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
            var posterURLNode = movieDetailNode.SelectSingleNode("../div[@class=\"lister-item-image ribbonize\"]/a/img[@src]");
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
            //Todo implement full scraping of a page
            return new Movie();
        }

        private static string ToQueryString(NameValueCollection nvc)
        {
            var array = (from key in nvc.AllKeys from value in nvc.GetValues(key) select $"{HttpUtility.UrlEncode(key)}={HttpUtility.UrlEncode(value)}").ToArray();
            return "?" + string.Join("&", array);
        }
    }
}
