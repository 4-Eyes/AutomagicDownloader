using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaAPIs.IMDb;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new IMDbClient();
            var movie = client.GetMovieAsync("tt3544112").Result;
//            var movies = client.GetPublicRatingsAsync("ur45902278", MovieView.Detail).Result;
//            var sadnessLevel = new TimeSpan();
//            var totalIMDbRating = 0.0;
//            var totalUserRating = 0.0;
//            var itemCount = 0;
//            movies.ForEach(item =>
//            {
//                var imdbItem = item as IMDbMediaItem;
//                if (imdbItem != null && imdbItem.RunTime != TimeSpan.Zero && imdbItem.Type == MediaType.Feature)
//                {
//                    sadnessLevel = sadnessLevel.Add(imdbItem.RunTime);
//                    totalIMDbRating += imdbItem.Rating;
//                    totalUserRating += imdbItem.UserRating;
//                    itemCount++;
//                }
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
            Console.ReadKey();
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
