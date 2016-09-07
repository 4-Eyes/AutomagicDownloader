using System;
using System.ComponentModel;
using System.Linq;

namespace MediaAPIs.IMDB
{
    /// <summary>
    /// Movie classifications for the US. 
    /// </summary>
    public enum USMovieClassification
    {
        [Description("General Audiences")]
        [Detail("All ages admitted. Nothing that would offend parents for viewing by children.")]
        G,
        [Description("Parental Guidance")]
        [Detail("Some material may not be suitable for children. Parents urged to give \"parental guidance\". May contain some material parents might not like for their young children.")]
        PG,
        [Description("Parental Guidance 13")]
        [Detail("Some material may be inappropriate for children under 13. Parents are urged to be cautious. Some material may be inappropriate for pre-teenagers.")]
        PG13,
        [Description("Restricted")]
        [Detail("Under 17 requires accompanying parent or adult guardian. Contains some adult material. Parents are urged to learn more about the film before taking their young children with them.")]
        R,
        [Description("Adults Only")]
        [Detail("No One 17 and Under Admitted. Clearly adult. Children are not admitted.")]
        NC17
    }

    /// <summary>
    /// Movie classifcations for NZ.
    /// </summary>
    public enum NZMovieClassification
    {
        [Description("General")]
        [Detail("Suitable for General Audiences. Anyone can be shown or sold this. G films should have very low levels of things like frightening scenes. However, not all G level films are intended for family audiences and it is always a good idea to look at reviews and plot information before taking children to any film")]
        G,
        [Description("Parental Guidance")]
        [Detail("Parental Guidance Recommended for Younger Viewers. Films and games with a PG label can be sold, hired, or shown to anyone. The PG label means guidance from a parent or guardian is recommended for younger viewers. It is important to remember that PG films can be aimed at an adult audience and to be aware of the content of a film if you are taking children to it")]
        PG,
        [Description("Mature")]
        [Detail("Films and games with an M label can be sold, hired, or shown to anyone. Films with an M label are more suitable for mature audiences. When considering whether to let a child see an M-rated film, it’s a good idea to find out what the film is about – and to always remember to check the descriptive note.")]
        M,
        [Description("Restricted 13")]
        [Detail("Restricted to people over the age of 13")]
        R13,
        [Description("Restricted 15")]
        [Detail("Restricted to people over the age of 15")]
        R15,
        [Description("Restricted 16")]
        [Detail("Restricted to people over the age of 16")]
        R16,
        [Description("Restricted 18")]
        [Detail("Restricted to people over the age of 18")]
        R18
    }

    class Detail : Attribute
    {
        public string Text;

        public Detail(string text)
        {
            Text = text;
        }
    }

    public static class ClassificationHelper
    {
        public static Enum ParseClassification(string classificationString)
        {
            foreach (var usClassification in Enum.GetValues(typeof(USMovieClassification)).Cast<object>().Where(usClassification => string.Equals(usClassification.ToString(), classificationString.Replace("-", ""))))
            {
                return (USMovieClassification) usClassification;
            }
            foreach (var nzClassification in Enum.GetValues(typeof(NZMovieClassification)).Cast<object>().Where(usClassification => string.Equals(usClassification.ToString(), classificationString)))
            {
                return (NZMovieClassification) nzClassification;
            }
            return null;
        }
    }

    public static class EnumExtensionMethods
    {
        public static string GetDescription<T>(this T enumerationValue)
                    where T : struct
        {
            var type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("EnumerationValue must be of Enum type", nameof(enumerationValue));
            }

            var memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo.Length <= 0) return enumerationValue.ToString();
            var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attrs.Length > 0 ? ((DescriptionAttribute)attrs[0]).Description : enumerationValue.ToString();
        }

        public static string GetDetail<T>(this T enumerationValue)
            where T : struct 
        {
            var type = enumerationValue.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException("EnumerationValue must be of Enum type", nameof(enumerationValue));
            }

            var memberInfo = type.GetMember(enumerationValue.ToString());
            if (memberInfo.Length <= 0) return enumerationValue.ToString();
            var attrs = memberInfo[0].GetCustomAttributes(typeof(Detail), false);

            return attrs.Length > 0 ? ((Detail)attrs[0]).Text : null;
        }
    }
}