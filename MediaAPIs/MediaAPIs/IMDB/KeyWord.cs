namespace MediaAPIs.IMDb
{
    public class KeyWord
    {
        public string Words { get; set; }
        public int FoundHelpful { get; set; }
        public int TotalVotes { get; set; }
        public double Relevance => TotalVotes != 0 ? (double) FoundHelpful/TotalVotes : 0;
    }
}