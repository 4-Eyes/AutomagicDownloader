using System;
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
            movies.ForEach(item =>
            {
                var imdbItem = item as Movie;
                if (imdbItem != null)
                {
                    sadnessLevel = sadnessLevel.Add(imdbItem.RunTime);
                }
            });
            Console.ReadKey();
        }
    }
}
