using Chummer.Api.Enums;
using RecordSourceGenerator.Generated;
using System.Xml;

namespace Chummer.Api.Models.GlobalSettings
{
    [XmlRecord]
    public sealed partial record Pdf(FileInfo? ApplicationPath, PdfParametersStyle ParametersStyle, bool InsertPdfNotes)
    {
        public bool Equals(Pdf? other)
        {
            return other is not null
                && ApplicationPath?.FullName == other.ApplicationPath?.FullName
                && ParametersStyle == other.ParametersStyle
                && InsertPdfNotes == other.InsertPdfNotes;
        }

        public override int GetHashCode()
        {
            return ApplicationPath?.GetHashCode() ?? 0
                ^ ParametersStyle.GetHashCode()
                ^ InsertPdfNotes.GetHashCode();
        }

        private static partial FileInfo? ParseApplicationPath(XmlReader reader, FileInfo? defaultValue)
        {
            var path = reader.ReadInnerXml();
            if (string.IsNullOrWhiteSpace(path))
                return defaultValue;
            return new FileInfo(path);
        }

        private static partial void WriteApplicationPath(FileInfo path, XmlWriter writer, string elementName)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(path.FullName);
            writer.WriteEndElement();
        }
    }
}
