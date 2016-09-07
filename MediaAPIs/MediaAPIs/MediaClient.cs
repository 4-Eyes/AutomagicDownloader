using System.Net.Http;

namespace MediaAPIs
{
    public abstract class MediaClient
    {
        protected HttpClient Client;
        protected HttpClientHandler Handler;
    }
}