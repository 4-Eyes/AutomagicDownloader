using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaAPIs.IMDB;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new IMDBClient();
            var movies = client.GetPublicRatingsAsync("ur45902278", MovieView.Detail).Result;
            var sadnessLevel = new TimeSpan();
            var totalIMDbRating = 0.0;
            var totalUserRating = 0.0;
            var itemCount = 0;
            movies.ForEach(item =>
            {
                var imdbItem = item as Movie;
                if (imdbItem != null && imdbItem.RunTime != TimeSpan.Zero && imdbItem.Type != MediaType.MiniSeries && imdbItem.Type != MediaType.TVSeries && imdbItem.Type != MediaType.TVEpisode)
                {
                    sadnessLevel = sadnessLevel.Add(imdbItem.RunTime);
                    totalIMDbRating += imdbItem.Rating;
                    totalUserRating += imdbItem.UserRating;
                    itemCount++;
                }
            });
            var aveRating = totalIMDbRating/itemCount;
            var aveUserRating = totalUserRating/itemCount;
            var featureFilms =
                movies.Where(item => (item as Movie).Type == MediaType.Feature).Select(thing => thing.Title).ToList();
            featureFilms.Sort();
            var duplicates = featureFilms.GroupBy(x => x)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key).ToList();
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
