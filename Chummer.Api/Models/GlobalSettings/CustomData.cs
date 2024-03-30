namespace Chummer.Api.Models.GlobalSettings
{
    public sealed record CustomData(bool AllowLiveUpdates, IReadOnlyList<DirectoryInfo> CustomDataDirectories)
    {
        public bool Equals(CustomData? other)
        {
            return other is not null
                && AllowLiveUpdates == other.AllowLiveUpdates
                && CustomDataDirectories.Count == other.CustomDataDirectories.Count
                && CustomDataDirectories
                    .Zip(other.CustomDataDirectories)
                    .All(a => a.First.FullName == a.Second.FullName);
        }

        public override int GetHashCode()
        {
            return AllowLiveUpdates.GetHashCode() ^ CustomDataDirectories
                .Select(d => d.FullName.GetHashCode()).Aggregate(0, (l, r) => l ^ r);

        }
    }
}
