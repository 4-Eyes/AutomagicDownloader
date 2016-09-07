using System.Collections.Generic;
using System.Net.Http;
using System.Xml;

namespace MediaAPIs.MAL
{
    public class MALClient : MediaClient
    {
        private readonly string _animelistURL = "http://myanimelist.net/malappinfo.php?status=all&u={0}";

        public MALClient(HttpClientHandler newHandler = null)
        {
            Handler = newHandler ?? new HttpClientHandler();
            Client = new HttpClient(Handler);
        }

        public List<Anime> GetAnimeList(string user)
        {
            var animeListXML = Client.GetStringAsync(string.Format(_animelistURL, user)).Result;
            var listXMLDoc = new XmlDocument();
            listXMLDoc.LoadXml(animeListXML);
            //var userDetails = 
            return new List<Anime>();
        }
    }
}