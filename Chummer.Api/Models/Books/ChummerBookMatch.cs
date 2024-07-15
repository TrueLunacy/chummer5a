using RecordSourceGenerator.Generated;
using System.Globalization;
using System.Xml;

namespace Chummer.Api.Models.Books
{
    [XmlRecord(ElementName = "match")]
    public sealed partial record ChummerBookMatch(
        [property: XmlAsElement(ElementName = "language")] CultureInfo Language,
        [property: XmlAsElement(ElementName = "text")] string Text,
        [property: XmlAsElement(ElementName = "page")] int Page
    )
    {
        private static partial CultureInfo ParseLanguage(XmlReader reader, CultureInfo? defaultValue)
        {
            var culture = reader.ReadInnerXml();
            if (string.IsNullOrWhiteSpace(culture))
                return defaultValue;
            try
            {
                return CultureInfo.GetCultureInfo(culture);
            }
            catch (CultureNotFoundException)
            {
                return defaultValue;
            }
        }

        private static partial void WriteLanguage(CultureInfo info, XmlWriter writer, string elementName)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(info.Name);
            writer.WriteEndElement();
        }

    }
}
