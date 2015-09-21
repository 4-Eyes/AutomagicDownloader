using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaAPIs.IMDB;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new IMDBClient();
            var movies = client.GetPublicRatingsAsync("ur45902278").Result;
            Console.ReadKey();
        }
    }
}
