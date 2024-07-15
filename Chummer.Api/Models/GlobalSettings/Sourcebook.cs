using RecordSourceGenerator.Generated;
using System.Xml;

namespace Chummer.Api.Models.GlobalSettings
{
    [XmlRecord]
    public sealed partial record Sourcebook(string Key, FileInfo Path, int PageOffset)
    {
        public bool Equals(Sourcebook? other)
        {
            return other != null
                && Key == other.Key
                && Path.FullName == other.Path.FullName
                && PageOffset == PageOffset;
        }

        public override int GetHashCode()
        {
            // no idea if this is a good hash code, but it'll do the job and be relatively cheap
            return Key.GetHashCode() ^ Path.FullName.GetHashCode() ^ PageOffset.GetHashCode();
        }

        private static partial FileInfo? ParsePath(XmlReader reader, FileInfo? defaultValue)
        {
            var path = reader.ReadInnerXml();
            if (string.IsNullOrWhiteSpace(path))
                return defaultValue;
            return new FileInfo(path);
        }

        private static partial void WritePath(FileInfo path, XmlWriter writer, string elementName)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(path.FullName);
            writer.WriteEndElement();
        }
    }
}
