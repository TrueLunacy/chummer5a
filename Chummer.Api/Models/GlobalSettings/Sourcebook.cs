namespace Chummer.Api.Models.GlobalSettings
{
    public sealed record Sourcebook(string Key, FileInfo Path, int PageOffset)
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
    }
}
