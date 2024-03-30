using Chummer.Api.Enums;

namespace Chummer.Api.Models.GlobalSettings
{
    public sealed record Pdf(FileInfo? ApplicationPath, PdfParametersStyle ParametersStyle, bool InsertPdfNotes)
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
    }
}
