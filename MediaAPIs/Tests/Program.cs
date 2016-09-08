using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaAPIs.IMDb;
using Newtonsoft.Json;

namespace Tests
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var foo = DoStuff().Result;
            Console.ReadKey();
        }

        private static async Task<int> DoStuff()
        {
            var client = new IMDbClient();
            //            var foo =
            //                client.ResolveRedirects(
            //                    "http://www.imdb.com/offsite/?page-action=offsite-theboyandthebeast&token=BCYlpaehnn27QsRt0lJvACPR0rxn_NIgqMjIuxg-UGE3OdtaDwRKXj-8o9AUXKoD15ZjEstTyDhZ%0D%0AsbwtnrLtDxo1ptO262hACNsFAGe8asUB2Plh07m6ZaVFS-yeTsHe0tkd3fRUvyb30j6MCQTxVGWt%0D%0AX9sChlPyIbeCatNee1i_TZ2YZKziyfi5EysVkhjdiv-1CkcQAZrRob6wDL9GoiLafZjiswNgg5Xz%0D%0ANgWq19m8Pnc%0D%0A&ref_=tt_pdt_ofs_offsite_0");
//            var movie = client.GetMovieAsync("tt2936180").Result;
            //tt4272866 (Bakemono no Ko) tt3544112 (Sing Street) tt2936180 (Far from Men) tt0468569 (The Dark Knight)
            var movies = client.GetPublicRatingsAsync("ur45902278", MovieView.Detail).Result;
            var featureFilms = movies.Where(item => (item as IMDbMediaItem).Type.Value == MediaType.Feature);
            var fullMovies = new List<IMDbMediaItem>();
//            var tasks = featureFilms.Select(featureFilm => Task.Run(async () =>
//            {
//                var fullMovie = await client.GetMovieAsync(featureFilm.Id);
//                fullMovie.UserRating = featureFilm.UserRating;
//                if (!fullMovie.Title.Equals(featureFilm.Title))
//                {
//                    fullMovie.OtherTitles.Add(featureFilm.Title);
//                }
//                fullMovies.Add(fullMovie);
//            })).ToList();
//            while (tasks.Count > 0)
//            {
//                var finishedTask = await Task.WhenAny(tasks);
//
//                tasks.Remove(finishedTask);
//
//                await finishedTask;
//            }
            foreach (var featureFilm in featureFilms)
            {
                var fullMovie = await client.GetMovieAsync(featureFilm.Id);
                fullMovie.UserRating = featureFilm.UserRating;
                if (!fullMovie.Title.Equals(featureFilm.Title))
                {
                    fullMovie.OtherTitles.Add(featureFilm.Title);
                }
                fullMovies.Add(fullMovie);
            }
            WriteDetailsToJSON(fullMovies);
            //            var sadnessLevel = new TimeSpan();
            //            var totalIMDbRating = 0.0;
            //            var totalUserRating = 0.0;
            //            var itemCount = 0;
            //            movies.ForEach(item =>
            //            {
            //                var imdbItem = item as IMDbMediaItem;
            //                if (imdbItem == null || imdbItem.RunTime == TimeSpan.Zero || imdbItem.Type != MediaType.Feature) return;
            //                if (imdbItem.RunTime != null) sadnessLevel = sadnessLevel.Add(imdbItem.RunTime.Value);
            //                totalIMDbRating += imdbItem.Rating;
            //                totalUserRating += imdbItem.UserRating;
            //                itemCount++;
            //            });
            //            var aveRating = totalIMDbRating/itemCount;
            //            var aveUserRating = totalUserRating/itemCount;
            //            var featureFilms =
            //                movies.Where(item => (item as IMDbMediaItem).Type == MediaType.Feature).Select(thing => thing.Title).ToList();
            //            featureFilms.Sort();
            //            var duplicates = featureFilms.GroupBy(x => x)
            //                .Where(group => group.Count() > 1)
            //                .Select(group => group.Key).ToList();
            //            WriteNamesToFile(featureFilms);
            return 0;
        }

        private static void WriteDetailsToJSON(List<IMDbMediaItem> fullMovies)
        {
            if (File.Exists(@"C:\Users\James\Desktop\MovieDump.json"))
            {
                File.WriteAllText(@"C:\Users\James\Desktop\MoviesDump.json", string.Empty);
            }
            using (var writer = new StreamWriter(@"C:\Users\James\Desktop\MoviesDump.json"))
            {
                writer.Write(JsonConvert.SerializeObject(fullMovies));
            }
        }

        private static void WriteNamesToFile(List<string> featureFilms)
        {
            if (File.Exists(@"C:\Users\James\Desktop\Movies.txt"))
            {
                File.WriteAllText(@"C:\Users\James\Desktop\Movies.txt", string.Empty);
            }
            using (var writer = new StreamWriter(@"C:\Users\James\Desktop\Movies.txt"))
            {
                foreach (var featureFilm in featureFilms)
                {
                    writer.Write(featureFilm + "\n");
                }
            }
        }
    }
}