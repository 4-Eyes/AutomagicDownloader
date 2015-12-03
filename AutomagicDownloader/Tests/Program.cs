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
            var totalIMDbRating = 0.0;
            var totalUserRating = 0.0;
            var itemCount = 0;
            movies.ForEach(item =>
            {
                var imdbItem = item as Movie;
                if (imdbItem != null)
                {
                    sadnessLevel = sadnessLevel.Add(imdbItem.RunTime);
                    totalIMDbRating += imdbItem.Rating;
                    totalUserRating += imdbItem.UserRating;
                    itemCount++;
                }
            });
            var aveRating = totalIMDbRating/itemCount;
            var aveUserRating = totalUserRating/itemCount;
            Console.ReadKey();
        }
    }
}
