using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Xml;
using Chummer.Api.Enums;
using RecordSourceGenerator.Generated;

namespace Chummer.Api.Models.GlobalSettings
{
    [XmlRecord]
    public sealed partial record GlobalSettings(
        Update Update,
        CustomData CustomData,
        Pdf Pdf,
        Print Print,
        Display Display,
        UX UX,
        Saving Saving,
        Logging Logging,
        Character Character,
        CultureInfo Language,
        ImmutableArray<FileInfo> MostRecentlyUsed,
        ImmutableArray<FileInfo> FavoriteCharacters,
        ImmutableArray<Sourcebook> SourcebookInfo
    )
    {
        private static partial CultureInfo ParseLanguage(XmlReader reader, CultureInfo? defaultValue)
        {
            var culture = reader.ReadInnerXml();
            if (string.IsNullOrWhiteSpace(culture))
                return defaultValue;
            try
            {
                return CultureInfo.GetCultureInfo(culture);
            }
            catch (CultureNotFoundException)
            {
                return defaultValue;
            }
        }

        private static partial FileInfo ParseMostRecentlyUsed(XmlReader reader, FileInfo defaultValue)
        {
            var path = reader.ReadInnerXml();
            if (string.IsNullOrWhiteSpace(path))
                return defaultValue;
            return new FileInfo(path);
        }

        private static partial FileInfo ParseFavoriteCharacters(XmlReader reader, FileInfo defaultValue)
        {
            var path = reader.ReadInnerXml();
            if (string.IsNullOrWhiteSpace(path))
                return defaultValue;
            return new FileInfo(path);
        }

        private static partial void WriteLanguage(CultureInfo info, XmlWriter writer, string elementName)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(info.Name);
            writer.WriteEndElement();
        }

        private static partial void WriteMostRecentlyUsed(FileInfo path, XmlWriter writer, string elementName)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(path.FullName);
            writer.WriteEndElement();
        }

        private static partial void WriteFavoriteCharacters(FileInfo path, XmlWriter writer, string elementName)
        {
            writer.WriteStartElement(elementName);
            writer.WriteValue(path.FullName);
            writer.WriteEndElement();
        }

        public bool Equals(GlobalSettings? other)
        {
            return other is not null
                && Update == other.Update
                && CustomData == other.CustomData
                && Pdf == other.Pdf
                && Print == other.Print
                && Display == other.Display
                && UX == other.UX
                && Saving == other.Saving
                && Logging == other.Logging
                && Character == other.Character
                && Language?.Name == other.Language?.Name
                && MostRecentlyUsed.SequenceEqual(other.MostRecentlyUsed)
                && FavoriteCharacters.SequenceEqual(other.FavoriteCharacters)
                && SourcebookInfo.SequenceEqual(other.SourcebookInfo);
        }

        public override int GetHashCode()
        {
            return Update.GetHashCode()
                ^ CustomData.GetHashCode()
                ^ Pdf.GetHashCode()
                ^ Print.GetHashCode()
                ^ Display.GetHashCode()
                ^ UX.GetHashCode()
                ^ Saving.GetHashCode()
                ^ Logging.GetHashCode()
                ^ Character.GetHashCode()
                ^ (Language?.GetHashCode() ?? 0)
                ^ MostRecentlyUsed.Select(mru => mru.FullName.GetHashCode()).Aggregate((l, r) => l ^ r)
                ^ FavoriteCharacters.Select(f => f.FullName.GetHashCode()).Aggregate((l, r) => l ^ r)
                ^ SourcebookInfo.Select(s => s.GetHashCode()).Aggregate((l, r) => l ^ r);
        }


        /*public static readonly GlobalSettings DefaultSettings = new(
            new Update(ShouldAutoUpdate: false, PreferNightly: false),
            new CustomData(AllowLiveUpdates: false, CustomDataDirectories: new List<DirectoryInfo>()),
            new Pdf(ApplicationPath: null, ParametersStyle: PdfParametersStyle.WebBrowserStyle, InsertPdfNotes: true),
            new Print(PrintToFileFirst: false, PrintZeroRatingSkills: false, PrintExpenses: PrintExpenses.NoPrint,
                PrintNotes: false, DefaultPrintSheet: "Shadowrun 5 (Skills grouped by Rating greater 0)"),
            new Display(StartInFullscreenMode: false, ColorMode: ColorMode.Automatic, DpiScalingMethod: DpiScalingMethod.None,
                CustomDateFormat: null, CustomTimeFormat: null),
            new UX(SearchRestrictedToCurrentCategory: true, AskConfirmDelete: true, AskConfirmKarmaExpense: true,
                HideItemsOverAvailabilityLimitInCreate: true, AllowEasterEggs: false, HideMasterIndex: false,
                HideCharacterRoster: false, SingleDiceRoller: true, AllowScrollIncrement: false,
                AllowScrollTabSwitch: false, AllowSkillDiceRolling: true, SetTimeWithDate: true,
                DefaultMasterIndexSettingsFile: Guid.Parse("67e25032-2a4e-42ca-97fa-69f7f608236c")),
            new Saving(SaveCompressionLevel: CompressionLevel.Balanced, ImageCompressionLevel: ImageCompression.Png,
                LastMugshotFolder: null),
            new Logging(LogLevel: LogLevel.NoLogging, LoggingResetCountdown: 0),
            new Character(RosterPath: null, CreateBackupOnCareer: true,
                DefaultSettingsFile: Guid.Parse("223a11ff-80e0-428b-89a9-6ef1c243b8b6"), LiveRefresh: false,
                EnableLifeModules: false),
            Language: CultureInfo.GetCultureInfo("en-us"),
            MostRecentlyUsed: new List<FileInfo>(),
            FavoriteCharacters: new List<FileInfo>(),
            SourcebookInfo: new List<Sourcebook>()
        );*/
    }
}
