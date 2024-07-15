using Chummer.Api.Enums;
using Chummer.Api.Models.GlobalSettings;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Chummer.Api
{
    public class GlobalSettingsManager : IGlobalSettingsManager
    {
        public GlobalSettings DefaultGlobalSettings => new(
            new Update(ShouldAutoUpdate: false, PreferNightly: false),
            new CustomData(AllowLiveUpdates: false, CustomDataDirectories: []),
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
            MostRecentlyUsed: [],
            FavoriteCharacters: [],
            SourcebookInfo: []
        );

        public GlobalSettings LoadGlobalSettings(Stream stream)
        {
            XmlReader reader = XmlReader.Create(stream);
            return GlobalSettings.Read(reader, DefaultGlobalSettings);
        }

        public void SerializeGlobalSettings(GlobalSettings globalSettings, Stream stream)
        {
            XmlWriter writer = XmlWriter.Create(stream);
            globalSettings.Write(writer);
            writer.Flush();
        }
    }
}
