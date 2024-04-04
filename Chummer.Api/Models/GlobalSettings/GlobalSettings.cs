using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Chummer.Api.Enums;

namespace Chummer.Api.Models.GlobalSettings
{
    public record GlobalSettings(
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
        List<FileInfo> MostRecentlyUsed,
        List<FileInfo> FavoriteCharacters,
        List<Sourcebook> SourcebookInfo
    )
    {
        public int SettingsVersion => 1;

        public static readonly GlobalSettings DefaultSettings = new(
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
        );
    }
}
