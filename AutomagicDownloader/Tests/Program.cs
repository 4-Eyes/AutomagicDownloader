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
            var movies = client.GetWatchList("ur45902278");
            Console.WriteLine(USMovieClassification.G.GetDescription());
            Console.WriteLine(USMovieClassification.G.GetDetail());
            Console.ReadKey();
        }
    }
}
