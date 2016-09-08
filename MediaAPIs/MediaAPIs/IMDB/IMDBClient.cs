using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

namespace MediaAPIs.IMDb
{
    public class IMDbClient : MediaClient
    {
        /// <summary>
        ///     The url that is used for getting a public watchlist.
        /// </summary>
        private const string WatchListUrl = "http://www.imdb.com/user/{0}/watchlist";

        /// <summary>
        ///     The url that is used to any imdb title (i.e. tt2396224)
        /// </summary>
        private const string TitleUrl = "http://www.imdb.com/title/{0}";

        /// <summary>
        ///     The url used to get user ratings (as long as the list is public)
        /// </summary>
        private const string RatingsListUrl = "http://www.imdb.com/user/{0}/ratings";

        /// <summary>
        ///     The url for a users main page.
        /// </summary>
        private const string UserUrl = "http://www.imdb.com/user/{0}";

        private const string BaseUrl = "http://www.imdb.com";

        public IMDbClient(HttpClientHandler newHandler = null)
        {
            Handler = newHandler ?? new HttpClientHandler();
            Client = new HttpClient(Handler) {Timeout = TimeSpan.FromMinutes(10)};
        }

        public async Task<List<MediaItem>> GetPublicRatingsAsync(string user, MovieView view = MovieView.Compact)
        {
            var ratedMovies = new List<MediaItem>();

            var tasks = new List<Task>();
            // Get the number of ratings
            var userPageHTML = await Client.GetStringAsync(string.Format(UserUrl, user));
            var doc = new HtmlDocument();
            doc.LoadHtml(userPageHTML);
            var numberRatingsNode = doc.DocumentNode.SelectSingleNode($"//a[@href=\"/user/{user}/ratings\"]");
            if (numberRatingsNode == null) throw new Exception("User has no ratings or their ratings aren\"t public");
            var totalRatingsMatch = Regex.Match(numberRatingsNode.InnerText, "(?<num>[0-9,]+)");
            var totalRatings = int.Parse(totalRatingsMatch.Groups["num"].Value.Replace(",", ""));

            var start = 1;
            // Set up all the requests needed to get the list as individual tasks.
            do
            {
                var headers = new NameValueCollection
                {
                    {"view", view.ToString().ToLower()}, //Other options include compact, detail and grid
                    {"sort", "title:asc"}, //Other options rate_date:(desc|asc)...
                    {"defaults", "1"},
                    {"start", start.ToString()} //Increment this in 100\"s till no movies can be parsed
                };
                tasks.Add(Task.Run(async () =>
                {
                    var movies = await ParseRatingPage(user, view, headers);
                    if (movies == null) return;
                    ratedMovies.AddRange(movies);
                }));

                start += view.GetInterval();
            } while (totalRatings > start);

            // Now wait for each task to complete
            while (tasks.Count > 0)
            {
                var finishedTask = await Task.WhenAny(tasks);

                tasks.Remove(finishedTask);

                await finishedTask;
            }

            return ratedMovies;
        }

        private async Task<IEnumerable<MediaItem>> ParseRatingPage(string user, MovieView view,
            NameValueCollection headers)
        {
            var userRatingsHTML =
                    await Client.GetStringAsync(string.Format(RatingsListUrl, user) + ToQueryString(headers));

            var doc = new HtmlDocument();
            doc.LoadHtml(userRatingsHTML);
            var unparsedMovies = doc.DocumentNode.SelectNodes(view.GetXPathQuery());
            if (unparsedMovies == null || unparsedMovies.Count == 0) return null;
            return
                unparsedMovies.Select(unparsedMovie => ParseRatingsListMovieHTML(unparsedMovie, view))
                    .Where(parsedMoive => parsedMoive != null);
        }

        public async Task<List<MediaItem>> GetPublicWatchListAsync(string user)
        {
            var watchListHTML = await Client.GetStringAsync(string.Format(WatchListUrl, user));
            var doc = new HtmlDocument();
            doc.LoadHtml(watchListHTML);
            var unparsedMovies = doc.DocumentNode.SelectNodes("//div[@class=\"lister-item-content\"]");
            if (unparsedMovies.Count == 0)
            {
                throw new ArgumentException(
                    "User either doesn\"t have anything in their watch list or their watchlist isn\"t public");
            }
            return unparsedMovies.Select(ParseWatchListMovieHTML).ToList();
        }

        private static MediaItem ParseRatingsListMovieHTML(HtmlNode movieDetailNode, MovieView view)
        {
            var movie = new IMDbMediaItem();
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
                        movie.Year = int.Parse(yearNode.InnerText);
                    }
                    var typeNode = movieDetailNode.SelectSingleNode("td[@class=\"title_type\"]");
                    if (typeNode != null)
                    {
                        foreach (
                            var value in
                                Enum.GetValues(typeof(MediaType))
                                    .Cast<object>()
                                    .Where(
                                        value =>
                                            value.ToString() == typeNode.InnerText.Replace("-", "").Replace(" ", "")))
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
                        movie.Title = HttpUtility.HtmlDecode(test.InnerText);
                        var match = Regex.Match(titleYearTypeNode.SelectSingleNode("span").InnerText,
                            "\\((?<year>[0-9]+).(?<type>[a-zA-z  -]+|)");
                        if (match.Success)
                        {
                            movie.Year = int.Parse(match.Groups["year"].Value);
                            object type = null;
                            if (string.IsNullOrEmpty(match.Groups["type"].Value))
                            {
                                type = MediaType.Feature;
                            }
                            else
                            {
                                foreach (
                                    var value in
                                        Enum.GetValues(typeof(MediaType))
                                            .Cast<object>()
                                            .Where(
                                                value =>
                                                    value.ToString() ==
                                                    match.Groups["type"].Value.Replace("-", "").Replace(" ", "")))
                                {
                                    type = (MediaType) value;
                                    break;
                                }
                            }
                            if (type != null)
                            {
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
            var movie = new IMDbMediaItem();
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
                movie.Genres.AddRange(genresNode.InnerText.Split(',').Select(s => s.Trim()).ToList());
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
                movie.ReleaseDate = !string.IsNullOrWhiteSpace(yearString)
                    ? new DateTime(int.Parse(yearString), 1, 1)
                    : DateTime.MinValue;
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

        public string ResolveRedirects(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.AllowAutoRedirect = true;
            req.Timeout = 20000;

            try
            {
                var myResp = (HttpWebResponse) req.GetResponse();
                return myResp.ResponseUri.AbsoluteUri;
            }
            catch (WebException)
            {
                return null;
            }
        }

        public async Task<IMDbMediaItem> GetMovieAsync(string imdbId)
        {
            var movie = new IMDbMediaItem
            {
                Id = imdbId,
                Type = MediaType.Feature
            };
            var tasks = new List<Task>();

            
            {
                var pageHtml = Client.GetStringAsync(string.Format(TitleUrl, imdbId)).Result;
                var doc = new HtmlDocument();
                doc.LoadHtml(pageHtml);
                var titleBarNode = doc.DocumentNode.SelectSingleNode("//div[@class=\"title_bar_wrapper\"]");
                tasks.Add(Task.Run(() =>
                {
                    var titleYearString = titleBarNode.SelectSingleNode("//h1[@itemprop=\"name\"]").InnerText;
                    var titleYear = Regex.Match(HttpUtility.HtmlDecode(titleYearString).Trim() ?? "",
                        "(?<title>.+).\\((?<year>[0-9]{4})\\)$");
                    movie.Title = titleYear.Groups["title"].Value;
                    movie.Year = int.Parse(titleYear.Groups["year"].Value);
                    var ratingsText = titleBarNode.SelectSingleNode("//div[@class='imdbRating']")
                        .InnerText.Replace("\n", "");
                    var ratingsDetails = Regex.Match(ratingsText, "(?<rating>[0-9\\.]{3})\\/1(0|1)[^0-9]+(?<votes>[0-9,]+)");
                    if (ratingsDetails.Success)
                    {
                        movie.Rating = double.Parse(ratingsDetails.Groups["rating"].Value);
                        movie.NumberOfVotes = int.Parse(ratingsDetails.Groups["votes"].Value.Replace(",", ""));
                    }
                    // Todo if I ever doing logging in then I should add scraping of the user rating.
                    var originalTitleNode = titleBarNode.SelectSingleNode("//div[@class='originalTitle']");
                    if (originalTitleNode != null)
                    {
                        var originalTitle = Regex.Match(originalTitleNode.InnerText, "(?<title>.+) \\(original title\\)");
                        movie.OtherTitles.Add(originalTitle.Groups["title"].Value);
                    }
                    var posterNode = titleBarNode.ParentNode.SelectSingleNode("//div[@class='poster']/a/img");
                    if (posterNode != null)
                    {
                        movie.PosterURL = posterNode.Attributes["src"].Value;
                    }
                }));
                var generalDetailsNode =
                    doc.DocumentNode.SelectSingleNode("//div[@class=\"minPosterWithPlotSummaryHeight\"]");
                tasks.Add(Task.Run(() =>
                {
                    if (generalDetailsNode == null) return;
                    var imgNode = generalDetailsNode.SelectSingleNode("div/div/a/img");
                    if (imgNode != null)
                    {
                        movie.PosterURL = imgNode.Attributes["src"].Value;
                    }
                    var metacriticNode = generalDetailsNode.SelectSingleNode("//div[contains(@class,'metacriticScore')]");
                    if (metacriticNode != null)
                    {
                        movie.MetacriticScore = int.Parse(metacriticNode.InnerText);
                    }
                    var shortSummary = generalDetailsNode.SelectSingleNode("//div[@class='summary_text']");
                    if (shortSummary != null)
                    {
                        movie.ShortSummary = shortSummary.InnerText.Replace("\n", "").Trim();
                    }
                }));
                var otherGeneralDetailsNode = doc.DocumentNode.SelectSingleNode("//div[@class='plot_summary_wrapper']");
                tasks.Add(Task.Run(() =>
                {
                    if (otherGeneralDetailsNode == null) return;
                    var metacriticNode =
                        otherGeneralDetailsNode.SelectSingleNode("//div[contains(@class,'metacriticScore')]");
                    if (metacriticNode != null)
                    {
                        movie.MetacriticScore = int.Parse(metacriticNode.InnerText.Replace("\n", ""));
                    }
                    var shortSummary = otherGeneralDetailsNode.SelectSingleNode("//div[@class='summary_text']");
                    if (shortSummary != null)
                    {
                        movie.ShortSummary = shortSummary.InnerText.Replace("\n", "").Trim();
                    }
                }));
                var storylineDetailsNode = doc.DocumentNode.SelectSingleNode("//div[@id=\"titleStoryLine\"]");
                tasks.Add(Task.Run(() =>
                {
                    var summary = storylineDetailsNode.SelectSingleNode("div[@itemprop='description']");
                    if (summary != null)
                    {
                        movie.Synopsis = summary.InnerText.Replace("\n", "").Trim();
                    }
                    var genres = storylineDetailsNode.SelectSingleNode("//div[@itemprop='genre']");
                    if (genres != null)
                    {
                        movie.Genres.AddRange(
                            genres.InnerText.Replace("\n", "")
                                .Trim()
                                .Replace("&nbsp;", "")
                                .Replace("Genres: ", "")
                                .Split('|')
                                .Select(g => g.Trim()));
                    }
                    var certificateNode = storylineDetailsNode.SelectSingleNode("//span[@itemprop='contentRating']");
                    if (certificateNode != null)
                    {
                        var certificate = ClassificationHelper.ParseClassification(certificateNode.InnerText);
                        movie.Classification = certificate;
                    }
                }));
                var productionDetailsNode = doc.DocumentNode.SelectSingleNode("//div[@id=\"titleDetails\"]");
                tasks.Add(Task.Run(() =>
                {
                    var details = productionDetailsNode.SelectNodes("div[@class='txt-block']");
                    foreach (var detail in details)
                    {
                        var header = detail.SelectSingleNode("h4");
                        if (header == null) continue;
                        var headerText = header.InnerText.Replace("\n", "").Trim();
                        if (headerText.Contains("Official Sites"))
                        {
                            var links = detail.SelectNodes("a");
                            foreach (var link in links)
                            {
                                var website = ResolveRedirects(BaseUrl + link.Attributes["href"].Value);
                                if (website != null)
                                {
                                    movie.OfficialSites.Add(website);
                                }
                            }
                        }
                        else if (headerText.Contains("Country"))
                        {
                            var countries = detail.SelectNodes("a");
                            foreach (var country in countries)
                            {
                                movie.Countries.Add(country.InnerText.Replace("\n", "").Trim());
                            }
                        }
                        else if (headerText.Contains("Language"))
                        {
                            var languages = detail.SelectNodes("a");
                            foreach (var language in languages)
                            {
                                movie.Languages.Add(language.InnerText.Replace("\n", "").Trim());
                            }
                        }
                        else if (headerText.Contains("Release Date"))
                        {
                            var releaseDate = header.NextSibling.InnerText;
                            var r = Regex.Match(releaseDate, "(.+)\\([^\\)]+\\)");
                            if (!r.Success) continue;
                            var rd = DateTime.MinValue;
                            DateTime.TryParse(r.Groups[1].Value, out rd);
                            if (!rd.Equals(DateTime.MinValue))
                            {
                                movie.ReleaseDate = rd;
                            }
                        }
                        else if (headerText.Contains("Budget"))
                        {
                            var budget = new string (header.NextSibling.InnerText.Where(c => char.IsDigit(c) || c.Equals('.')) .ToArray());
                            movie.Budget = long.Parse(budget);
                        }
                        else if (headerText.Contains("Gross"))
                        {
                            var gross = header.NextSibling.InnerText;
                            movie.Gross = int.Parse(gross.Replace(",", "").Replace("$", ""));
                        }
                        else if (headerText.Contains("Runtime"))
                        {
                            var time = detail.SelectSingleNode("time").InnerText;
                            var rt = TimeSpan.MinValue;
                            TimeSpan.TryParse(time.Replace("min", ""), out rt);
                            if (!rt.Equals(TimeSpan.MinValue))
                            {
                                movie.RunTime = rt;
                            }
                        }
                        else if (headerText.Contains("Color"))
                        {
                            var colour = detail.SelectSingleNode("a").InnerText;
                            movie.Colour = colour.Replace("\n", "").Trim();
                        }
                    }
                }));
            }
            tasks.Add(Task.Run(() =>
            {
                var fullCreditsHtml = Client.GetStringAsync(string.Format(TitleUrl, imdbId) + "/fullcredits").Result;
                var doc = new HtmlDocument();
                doc.LoadHtml(fullCreditsHtml);
                var tables =
                    doc.DocumentNode.SelectNodes(
                        "//table[@class=\"simpleTable simpleCreditsTable\"]/tbody | //table[@class='cast_list']");
                foreach (var table in tables)
                {
                    var credits = table.SelectNodes("tr/td[@class=\"name\"] | tr/td[@itemprop='actor']").Select(node =>
                    {
                        string id = null;
                        if (node.SelectSingleNode("a")?.Attributes["href"] != null)
                        {
                            id = Regex.Match(node.SelectSingleNode("a").Attributes["href"].Value, "nm[0-9]+").Value;
                        }
                        var credit = new Credit
                        {
                            Id = id == "" ? null : id,
                            Name = node.InnerText.Replace("\n", "").Trim()
                        };
                        return credit;
                    });
                    var group = table.PreviousSibling.PreviousSibling.Name == "h4"
                        ? table.PreviousSibling.PreviousSibling.InnerText
                        : table.ParentNode.PreviousSibling.PreviousSibling.InnerText;
                    group = group.Replace("\n", "");
                    if (@group.Contains("Directed"))
                    {
                        movie.Directors.AddRange(credits);
                    }
                    else if (@group.Contains("Writing"))
                    {
                        movie.Writers.AddRange(credits);
                    }
                    else if (@group.Contains("Cast "))
                    {
                        movie.Cast.AddRange(credits);
                    }
                    else if (@group.Contains("Produced"))
                    {
                        movie.Producers.AddRange(credits);
                    }
                    else if (@group.Contains("Music by"))
                    {
                        movie.Composers.AddRange(credits);
                    }
                    else
                    {
                        movie.OtherCrew.AddRange(credits);
                    }
                }
            }));
            tasks.Add(Task.Run(() =>
            {
                var keywordsHtml = Client.GetStringAsync(string.Format(TitleUrl, imdbId) + "/keywords").Result;
                var keywordsDoc = new HtmlDocument();
                keywordsDoc.LoadHtml(keywordsHtml);
                var keywords = keywordsDoc.DocumentNode.SelectNodes("//td[@class='soda sodavote']");
                if (keywords == null) return;
                foreach (var keyword in keywords)
                {
                    var k = new KeyWord();
                    var words =
                        keyword.SelectSingleNode("div[@class='sodatext']/a")
                            .InnerText.Replace("\n", "")
                            .Trim();
                    var relevance =
                        Regex.Match(
                            keyword.SelectSingleNode("div/div[@class='interesting-count-text']/a")
                                .InnerText.Replace("\n", "")
                                .Trim(),
                            "(?<helpful>[0-9]+) of (?<total>[0-9]+)");
                    k.Words = words;
                    if (relevance.Success)
                    {
                        k.FoundHelpful = int.Parse(relevance.Groups["helpful"].Value);
                        k.TotalVotes = int.Parse(relevance.Groups["total"].Value);
                    }
                    movie.Keywords.Add(k);
                }
            }));
            // Now wait for each task to complete
            while (tasks.Count > 0)
            {
                Task finishedTask;
                lock (tasks)
                {
                    finishedTask = Task.WhenAny(tasks).Result;
                }

                tasks.Remove(finishedTask);

                await finishedTask;
            }

            return movie;
        }

        private static string ToQueryString(NameValueCollection nvc)
        {
            var array = (from key in nvc.AllKeys
                from value in nvc.GetValues(key)
                select $"{HttpUtility.UrlEncode(key)}={HttpUtility.UrlEncode(value)}").ToArray();
            return "?" + string.Join("&", array);
        }
    }
}