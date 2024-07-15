using Chummer.Api.Enums;
using RecordSourceGenerator.Generated;
using System.Xml;

namespace Chummer.Api.Models.GlobalSettings
{
    [XmlRecord]
    public sealed partial record Saving(CompressionLevel SaveCompressionLevel, ImageCompression ImageCompressionLevel,
        DirectoryInfo? LastMugshotFolder)
    {
        public bool Equals(Saving? other)
        {
            return other is not null
                && SaveCompressionLevel == other.SaveCompressionLevel
                && ImageCompressionLevel == other.ImageCompressionLevel
                && LastMugshotFolder?.FullName == other.LastMugshotFolder?.FullName;
        }

        public override int GetHashCode()
        {
            return SaveCompressionLevel.GetHashCode()
                ^ ImageCompressionLevel.GetHashCode()
                ^ LastMugshotFolder?.GetHashCode() ?? 0;
        }

        private static partial DirectoryInfo? ParseLastMugshotFolder(XmlReader reader, DirectoryInfo? defaultValue)
        {
            var path = reader.ReadInnerXml();
            if (string.IsNullOrWhiteSpace(path))
                return defaultValue;
            return new DirectoryInfo(path);
        }

        private static partial void WriteLastMugshotFolder(DirectoryInfo path, XmlWriter writer, string elementName)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(path.FullName);
            writer.WriteEndElement();
        }
    }
}
