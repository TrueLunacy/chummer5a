using Chummer.Api.Enums;

namespace Chummer.Api.Models.GlobalSettings
{
    public sealed record Saving(CompressionLevel SaveCompressionLevel, ImageCompression ImageCompressionLevel,
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
    }
}
