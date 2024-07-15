using RecordSourceGenerator.Generated;
using System.Xml;

namespace Chummer.Api.Models.GlobalSettings
{
    [XmlRecord]
    public sealed partial record Character(DirectoryInfo? RosterPath, bool CreateBackupOnCareer, Guid DefaultSettingsFile,
        bool LiveRefresh, bool EnableLifeModules)
    {
        public bool Equals(Character? other)
        {
            return other is not null
                && RosterPath?.FullName == other.RosterPath?.FullName
                && CreateBackupOnCareer == other.CreateBackupOnCareer
                && DefaultSettingsFile == other.DefaultSettingsFile
                && LiveRefresh == other.LiveRefresh
                && EnableLifeModules == other.EnableLifeModules;
        }

        public override int GetHashCode()
        {
            // or-ing the bools probably isn't the best of ideas
            return RosterPath?.FullName.GetHashCode() ?? 0
                ^ CreateBackupOnCareer.GetHashCode()
                ^ DefaultSettingsFile.GetHashCode()
                ^ LiveRefresh.GetHashCode()
                ^ EnableLifeModules.GetHashCode();
        }

        private static partial DirectoryInfo? ParseRosterPath(XmlReader reader, DirectoryInfo? defaultValue)
        {
            var path = reader.ReadInnerXml();
            if (string.IsNullOrWhiteSpace(path))
                return defaultValue;
            return new DirectoryInfo(path);
        }

        private static partial void WriteRosterPath(DirectoryInfo path, XmlWriter writer, string elementName)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(path.FullName);
            writer.WriteEndElement();
        }
    }
}
