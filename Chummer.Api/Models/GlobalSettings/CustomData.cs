using RecordSourceGenerator.Generated;
using System.Collections.Immutable;
using System.Xml;

namespace Chummer.Api.Models.GlobalSettings
{
    [XmlRecord]
    public sealed partial record CustomData(bool AllowLiveUpdates, ImmutableArray<DirectoryInfo> CustomDataDirectories)
    {
        public bool Equals(CustomData? other)
        {
            return other is not null
                && AllowLiveUpdates == other.AllowLiveUpdates
                && CustomDataDirectories.Length == other.CustomDataDirectories.Length
                && CustomDataDirectories
                    .Zip(other.CustomDataDirectories)
                    .All(a => a.First.FullName == a.Second.FullName);
        }

        public override int GetHashCode()
        {
            return AllowLiveUpdates.GetHashCode() ^ CustomDataDirectories
                .Select(d => d.FullName.GetHashCode()).Aggregate(0, (l, r) => l ^ r);
        }

        private static partial DirectoryInfo? ParseCustomDataDirectories(XmlReader reader, DirectoryInfo? defaultValue)
        {
            var path = reader.ReadInnerXml();
            if (string.IsNullOrWhiteSpace(path))
                return defaultValue;
            return new DirectoryInfo(path);
        }

        private static partial void WriteCustomDataDirectories(DirectoryInfo path, XmlWriter writer, string elementName)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(path.FullName);
            writer.WriteEndElement();
        }
    }
}
