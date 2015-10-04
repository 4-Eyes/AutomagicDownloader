using System;
using System.ComponentModel;

namespace MediaAPIs.IMDB
{
    public enum MovieView
    {
        [IntervalSize(250)]
        [XPathSearch("//tr[@data-item-id]")]
        Compact,
        [IntervalSize(100)]
        [XPathSearch("//div[@class=\"list_item grid\"]")]
        Grid,
        [IntervalSize(100)]
        [XPathSearch("//div[@class=\"list_item odd\"] | //div[@class=\"list_item even\"]")]
        Detail
    }

    public enum MediaType
    {
        [Description("Documentary")]
        Documentary,
        [Description("Feature Film")]
        Feature,
        [Description("Mini Series")]
        MiniSeries,
        [Description("Short Film")]
        ShortFilm,
        [Description("TV Series")]
        TVSeries,
        [Description("TV Episode")]
        TVEpisode,
        [Description("TV Movie")]
        TVMovie,
        [Description("Video")]
        Video
    }

    class IntervalSize : Attribute
    {
        public int Size { get; set; }

        public IntervalSize(int size)
        {
            Size = size;
        }
    }

    class XPathSearch : Attribute
    {
        public string SearchString { get; set; }

        public XPathSearch(string searchString)
        {
            SearchString = searchString;
        }
    }

    public static class MovieViewHelper
    {
        public static int GetInterval(this MovieView enumerationValue)
        {
            var type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("EnumerationValue must be of Enum type", nameof(enumerationValue));
            }

            var memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo.Length <= 0) return 0;
            var attrs = memberInfo[0].GetCustomAttributes(typeof(IntervalSize), false);

            return attrs.Length > 0 ? ((IntervalSize)attrs[0]).Size : 0;
        }

        public static string GetXPathQuery(this MovieView enumerationValue)
        {
            var type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("EnumerationValue must be of Enum type", nameof(enumerationValue));
            }

            var memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo.Length <= 0) throw new ArgumentException("No XPath Query for this item");
            var attrs = memberInfo[0].GetCustomAttributes(typeof(XPathSearch), false);

            return attrs.Length > 0 ? ((XPathSearch)attrs[0]).SearchString : "";
        }
    }
}