namespace Chummer.Api.Models.GlobalSettings
{
    public sealed record Character(DirectoryInfo? RosterPath, bool CreateBackupOnCareer, Guid DefaultSettingsFile,
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
    }
}
