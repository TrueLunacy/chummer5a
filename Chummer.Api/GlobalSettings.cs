using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Chummer.Api
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
                HideAvailabilityCreation: true, AllowEasterEggs: false, HideMasterIndex: false,
                HideCharacterRoster: false, SingleDiceRoller: true, AllowScrollIncrement: false,
                AllowScrollTabSwitch: false, AllowSkillDiceRolling: true, SetTimeWithDate: true,
                DefaultMasterIndexSettingsFile: Guid.Parse("67e25032-2a4e-42ca-97fa-69f7f608236c")),
            new Saving(SaveCompressionLevel: CompressionLevel.Balanced, ImageCompressionLevel: ImageCompression.Png),
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

    public record Update(bool ShouldAutoUpdate, bool PreferNightly);
    public record CustomData(bool AllowLiveUpdates, IReadOnlyList<DirectoryInfo> CustomDataDirectories)
    {
        public object Single()
        {
            throw new NotImplementedException();
        }
    }

    public record Pdf(FileInfo? ApplicationPath, PdfParametersStyle ParametersStyle, bool InsertPdfNotes);
    public record Print(bool PrintToFileFirst, bool PrintZeroRatingSkills, PrintExpenses PrintExpenses,
        bool PrintNotes, string DefaultPrintSheet);
    public record Display(bool StartInFullscreenMode, ColorMode ColorMode, DpiScalingMethod DpiScalingMethod,
        string? CustomDateFormat, string? CustomTimeFormat);
    public record UX(bool SearchRestrictedToCurrentCategory, bool AskConfirmDelete,
            bool AskConfirmKarmaExpense, bool HideAvailabilityCreation, bool AllowEasterEggs,
            bool HideMasterIndex, bool HideCharacterRoster, bool SingleDiceRoller,
            bool AllowScrollIncrement, bool AllowScrollTabSwitch, bool AllowSkillDiceRolling,
            bool SetTimeWithDate, Guid DefaultMasterIndexSettingsFile);
    public record Saving(CompressionLevel SaveCompressionLevel, ImageCompression ImageCompressionLevel);
    public record Logging(LogLevel LogLevel, uint LoggingResetCountdown);
    public record Character(DirectoryInfo? RosterPath, bool CreateBackupOnCareer, Guid DefaultSettingsFile,
        bool LiveRefresh, bool EnableLifeModules);

    public record Sourcebook(string Key, FileInfo Path, int PageOffset);

    public enum PrintExpenses
    {
        Undefined,
        NoPrint,
        PrintValueExpenses,
        PrintAllExpenses
    }

    public enum ColorMode
    {
        Undefined,
        Automatic,
        Light,
        Dark
    }

    public enum PdfParametersStyle
    {
        Undefined,
        WebBrowserStyle,
        AcrobatStyle,
        AcrobatStyleNewInstance,
        UnixStyle,
        SumatraStyle,
        SumatraStyleReuseInstance
    }

    public enum CompressionLevel
    {
        Undefined,
        Fast,
        Balanced,
        Thorough
    }

    public enum ImageCompression
    {
        Undefined,
        Png,
        JpegAutomatic,
        JpegHigh = 90,
        JpegMedium = 80,
        JpegLow = 50,
        JpegExtraLow = 30,
        JpegExtraExtraLow = 10
    }

    public enum LogLevel
    {
        Undefined,
        NoLogging,
        OnlyLocal,
        OnlyMetric,
        Crashes,
        NotSet,
        Info,
        Trace
    }
    public enum DpiScalingMethod
    {
        Undefined,
        None,
        Zoom,       // System
        Rescale,    // PerMonitor/PerMonitorV2
        SmartZoom   // System (Enhanced)
    }

    public interface IGlobalSettingsManager
    {
        GlobalSettings LoadGlobalSettings(Stream stream);
        void SerializeGlobalSettings(GlobalSettings globalSettings, Stream stream);
    }

    public class GlobalSettingsManager : IGlobalSettingsManager
    {
        // todo: diagnostics for issues
        public GlobalSettings LoadGlobalSettings(Stream stream)
        {
            XDocument document;
            document = XDocument.Load(stream);
            int Version = document.Root?.ChildValue(nameof(GlobalSettings.SettingsVersion), 0) ?? 0;
            if (Version != 1)
                throw new InvalidOperationException("Settings file is not version 1");
            Update update = LoadUpdate(document.Root?.Single(nameof(GlobalSettings.Update)));
            CustomData cd = LoadCustomData(document.Root?.Single(nameof(GlobalSettings.CustomData)));
            Pdf pdf = LoadPdf(document.Root?.Single(nameof(GlobalSettings.Pdf)));
            Print print = LoadPrint(document.Root?.Single(nameof(GlobalSettings.Print)));
            Display display = LoadDisplay(document.Root?.Single(nameof(GlobalSettings.Display)));
            UX ux = LoadUx(document.Root?.Single(nameof(GlobalSettings.UX)));
            Saving saving = LoadSaving(document.Root?.Single(nameof(GlobalSettings.Saving)));
            Logging logging = LoadLogging(document.Root?.Single(nameof(GlobalSettings.Logging)));
            Character character = LoadCharacter(document.Root?.Single(nameof(GlobalSettings.Character)));
            string? lang = document.Root?.ChildValue(nameof(GlobalSettings.Language), (string?)null);
            CultureInfo language = lang is not null ? CultureInfo.GetCultureInfo(lang) : GlobalSettings.DefaultSettings.Language;
            List<FileInfo> mru = LoadCharacterList(document.Root?.Single(nameof(GlobalSettings.MostRecentlyUsed)));
            List<FileInfo> faves = LoadCharacterList(document.Root?.Single(nameof(GlobalSettings.FavoriteCharacters)));
            List<Sourcebook> sb = LoadSourcebooks(document.Root?.Single(nameof(GlobalSettings.SourcebookInfo)));
            return new GlobalSettings(update, cd, pdf, print, display, ux,
                saving, logging, character, language, mru, faves, sb);
        }

        private static Update LoadUpdate(XElement? update)
        {
            Update def = GlobalSettings.DefaultSettings.Update; 
            if (update is null)
                return def;
            return new Update(
                ShouldAutoUpdate: update.ChildValue(nameof(Update.ShouldAutoUpdate), def.ShouldAutoUpdate),
                PreferNightly: update.ChildValue(nameof(Update.PreferNightly), def.PreferNightly)
            );
        }

        private static CustomData LoadCustomData(XElement? data)
        {
            CustomData def = GlobalSettings.DefaultSettings.CustomData;
            if (data is null)
                return def;
            return new CustomData(
                AllowLiveUpdates: data.ChildValue(nameof(CustomData.AllowLiveUpdates), def.AllowLiveUpdates),
                CustomDataDirectories: ParseDirectories(data.Single(nameof(CustomData.CustomDataDirectories)))
            );
            IReadOnlyList<DirectoryInfo> ParseDirectories(XElement? directories)
            {
                if (directories is null)
                    return def.CustomDataDirectories;
                List<DirectoryInfo> list = new List<DirectoryInfo>();
                foreach (XElement directory in directories.Elements().Where(e => e.Name == "Directory"))
                {
                    string path = (string)directory;
                    DirectoryInfo dir = new DirectoryInfo(path);
                    list.Add(dir);
                }
                return list;
            }
        }

        private static Pdf LoadPdf(XElement? pdf)
        {
            Pdf def = GlobalSettings.DefaultSettings.Pdf;
            if (pdf is null)
                return def;
            return new Pdf(
                ApplicationPath: pdf.ChildValue(nameof(Pdf.ApplicationPath), def.ApplicationPath),
                ParametersStyle: pdf.ChildValueEnum(nameof(Pdf.ParametersStyle), def.ParametersStyle),
                InsertPdfNotes: pdf.ChildValue(nameof(Pdf.InsertPdfNotes), def.InsertPdfNotes)
            );
        }

        private static Print LoadPrint(XElement? print)
        {
            Print def = GlobalSettings.DefaultSettings.Print;
            if (print is null)
                return def;
            return new Print(
                PrintToFileFirst: print.ChildValue(nameof(Print.PrintToFileFirst), def.PrintToFileFirst),
                PrintZeroRatingSkills: print.ChildValue(nameof(Print.PrintZeroRatingSkills), def.PrintZeroRatingSkills),
                PrintExpenses: print.ChildValueEnum(nameof(Print.PrintExpenses), def.PrintExpenses),
                PrintNotes: print.ChildValue(nameof(Print.PrintNotes), def.PrintNotes),
                DefaultPrintSheet: print.ChildValue(nameof(Print.DefaultPrintSheet), def.DefaultPrintSheet)
            );
        }

        private static Display LoadDisplay(XElement? display)
        {
            Display def = GlobalSettings.DefaultSettings.Display;
            if (display is null)
                return def;
            return new Display(
                StartInFullscreenMode: display.ChildValue(nameof(Display.StartInFullscreenMode), def.StartInFullscreenMode),
                ColorMode: display.ChildValueEnum(nameof(Display.ColorMode), def.ColorMode),
                DpiScalingMethod: display.ChildValueEnum(nameof(Display.DpiScalingMethod), def.DpiScalingMethod),
                CustomDateFormat: display.ChildValue(nameof(Display.CustomDateFormat), def.CustomDateFormat),
                CustomTimeFormat: display.ChildValue(nameof(Display.CustomTimeFormat), def.CustomTimeFormat)
            );
        }

        private static UX LoadUx(XElement? ux)
        {
            UX def = GlobalSettings.DefaultSettings.UX;
            if (ux is null)
                return def;
            return new UX(
                SearchRestrictedToCurrentCategory: ux.ChildValue(nameof(UX.SearchRestrictedToCurrentCategory), def.SearchRestrictedToCurrentCategory),
                AskConfirmDelete: ux.ChildValue(nameof(UX.AskConfirmDelete), def.AskConfirmDelete),
                AskConfirmKarmaExpense: ux.ChildValue(nameof(UX.AskConfirmKarmaExpense), def.AskConfirmKarmaExpense),
                HideAvailabilityCreation: ux.ChildValue(nameof(UX.HideAvailabilityCreation), def.HideAvailabilityCreation),
                AllowEasterEggs: ux.ChildValue(nameof(UX.AllowEasterEggs), def.AllowEasterEggs),
                HideMasterIndex: ux.ChildValue(nameof(UX.HideMasterIndex), def.HideMasterIndex),
                HideCharacterRoster: ux.ChildValue(nameof(UX.HideCharacterRoster), def.HideCharacterRoster),
                SingleDiceRoller: ux.ChildValue(nameof(UX.SingleDiceRoller), def.SingleDiceRoller),
                AllowScrollIncrement: ux.ChildValue(nameof(UX.AllowScrollIncrement), def.AllowScrollIncrement),
                AllowScrollTabSwitch: ux.ChildValue(nameof(UX.AllowScrollTabSwitch), def.AllowScrollTabSwitch),
                AllowSkillDiceRolling: ux.ChildValue(nameof(UX.AllowSkillDiceRolling), def.AllowSkillDiceRolling),
                SetTimeWithDate: ux.ChildValue(nameof(UX.SetTimeWithDate), def.SetTimeWithDate),
                DefaultMasterIndexSettingsFile: ux.ChildValue(nameof(UX.DefaultMasterIndexSettingsFile), def.DefaultMasterIndexSettingsFile)
            );
        }

        private static Saving LoadSaving(XElement? saving)
        {
            Saving def = GlobalSettings.DefaultSettings.Saving;
            if (saving is null)
                return def;
            return new Saving(
                SaveCompressionLevel: saving.ChildValueEnum(nameof(Saving.SaveCompressionLevel), def.SaveCompressionLevel),
                ImageCompressionLevel: saving.ChildValueEnum(nameof(Saving.ImageCompressionLevel), def.ImageCompressionLevel)
            );
        }

        private static Logging LoadLogging(XElement? logging)
        {
            Logging def = GlobalSettings.DefaultSettings.Logging;
            if (logging is null)
                return def;
            return new Logging(
                LogLevel: logging.ChildValueEnum(nameof(Logging.LogLevel), def.LogLevel),
                LoggingResetCountdown: logging.ChildValue(nameof(Logging.LoggingResetCountdown), def.LoggingResetCountdown)
            );
        }

        private static Character LoadCharacter(XElement? character)
        {
            Character def = GlobalSettings.DefaultSettings.Character;
            if (character is null)
                return def;
            return new Character(
                RosterPath: character.ChildValue(nameof(Character.RosterPath), def.RosterPath),
                CreateBackupOnCareer: character.ChildValue(nameof(Character.CreateBackupOnCareer), def.CreateBackupOnCareer),
                DefaultSettingsFile: character.ChildValue(nameof(Character.DefaultSettingsFile), def.DefaultSettingsFile),
                LiveRefresh: character.ChildValue(nameof(Character.LiveRefresh), def.LiveRefresh),
                EnableLifeModules: character.ChildValue(nameof(Character.EnableLifeModules), def.EnableLifeModules)
            );
        }

        private static List<FileInfo> LoadCharacterList(XElement? mru)
        {
            if (mru is null)
                return GlobalSettings.DefaultSettings.MostRecentlyUsed;
            List<FileInfo> files = new List<FileInfo>();
            foreach (XElement file in mru.Elements().Where(e => e.Name == "File"))
            {
                string path = (string)file;
                FileInfo fi = new FileInfo(path);
                files.Add(fi);
            }
            return files;
        }

        private static List<Sourcebook> LoadSourcebooks(XElement? sb)
        {
            if (sb is null)
                return GlobalSettings.DefaultSettings.SourcebookInfo;
            List<Sourcebook> books = new List<Sourcebook>();
            foreach (XElement book in sb.Elements().Where(e => e.Name == nameof(Sourcebook)))
            {
                if (book.TryGetChildValue(nameof(Sourcebook.Key), out string? key)
                    && book.TryGetChildValue(nameof(Sourcebook.Path), out string? path)
                    && book.TryGetChildValue(nameof(Sourcebook.PageOffset), out int pageoffset))
                {
                    books.Add(new Sourcebook(key, new FileInfo(path), pageoffset));
                }
            }
            return books;
        }

        public void SerializeGlobalSettings(GlobalSettings globalSettings, Stream stream)
        {
            var xmlsettings = new XmlWriterSettings();
            xmlsettings.Indent = true;
            xmlsettings.CheckCharacters = true;
            xmlsettings.Encoding = Encoding.UTF8;
            using XmlWriter xml = XmlWriter.Create(stream, xmlsettings);
            using (var settings = xml.OpenElement(nameof(GlobalSettings)))
            {
                settings.WriteElementValue(nameof(GlobalSettings.SettingsVersion), globalSettings.SettingsVersion);
                using (var update = settings.OpenElement(nameof(GlobalSettings.Update)))
                    SerializeUpdate(globalSettings.Update, update);
                using (var cd = settings.OpenElement(nameof(GlobalSettings.CustomData)))
                    SerializeCustomData(globalSettings.CustomData, cd);
                using (var pdf = settings.OpenElement(nameof(GlobalSettings.Pdf)))
                    SerializePdf(globalSettings.Pdf, pdf);
                using (var print = settings.OpenElement(nameof(GlobalSettings.Print)))
                    SerializePrint(globalSettings.Print, print);
                using (var display = settings.OpenElement(nameof(GlobalSettings.Display)))
                    SerializeDisplay(globalSettings.Display, display);
                using (var ux = settings.OpenElement(nameof(GlobalSettings.UX)))
                    SerializeUx(globalSettings.UX, ux);
                using (var saving = settings.OpenElement(nameof(GlobalSettings.Saving)))
                    SerializeSaving(globalSettings.Saving, saving);
                using (var logging = settings.OpenElement(nameof(GlobalSettings.Logging)))
                    SerializeLogging(globalSettings.Logging, logging);
                using (var character = settings.OpenElement(nameof(GlobalSettings.Character)))
                    SerializeCharacter(globalSettings.Character, character);
                settings.WriteElementValue(nameof(GlobalSettings.Language), globalSettings.Language.Name);
                using (var mru = settings.OpenElement(nameof(GlobalSettings.MostRecentlyUsed)))
                    SerializeCharacterList(globalSettings.MostRecentlyUsed, mru);
                using (var faves = settings.OpenElement(nameof(GlobalSettings.FavoriteCharacters)))
                    SerializeCharacterList(globalSettings.FavoriteCharacters, faves);
                using (var sb = settings.OpenElement(nameof(GlobalSettings.SourcebookInfo)))
                    SerializeSourcebook(globalSettings.SourcebookInfo, sb);
            }
            xml.Flush();
        }

        private static void SerializeUpdate(Update update, XmlWriterElementHelper xml)
        {
            xml.WriteElementValue(nameof(Update.ShouldAutoUpdate), update.ShouldAutoUpdate);
            xml.WriteElementValue(nameof(Update.PreferNightly), update.PreferNightly);
        }

        private static void SerializeCustomData(CustomData cd, XmlWriterElementHelper xml)
        {
            xml.WriteElementValue(nameof(CustomData.AllowLiveUpdates), cd.AllowLiveUpdates);
            using (var cdd = xml.OpenElement(nameof(CustomData.CustomDataDirectories)))
            {
                foreach (var dir in cd.CustomDataDirectories)
                    xml.WriteElementValue("Directory", dir.FullName);
            }
        }

        private static void SerializePdf(Pdf pdf, XmlWriterElementHelper xml)
        {
            if (pdf.ApplicationPath is not null)
                xml.WriteElementValue(nameof(Pdf.ApplicationPath), pdf.ApplicationPath.FullName);
            xml.WriteElementValue(nameof(Pdf.ParametersStyle), pdf.ParametersStyle);
            xml.WriteElementValue(nameof(Pdf.InsertPdfNotes), pdf.InsertPdfNotes);
        }

        private static void SerializePrint(Print print, XmlWriterElementHelper xml)
        {
            xml.WriteElementValue(nameof(Print.PrintToFileFirst), print.PrintToFileFirst);
            xml.WriteElementValue(nameof(Print.PrintZeroRatingSkills), print.PrintZeroRatingSkills);
            xml.WriteElementValue(nameof(Print.PrintExpenses), print.PrintExpenses);
            xml.WriteElementValue(nameof(Print.PrintNotes), print.PrintNotes);
            xml.WriteElementValue(nameof(Print.DefaultPrintSheet), print.DefaultPrintSheet);
        }

        private static void SerializeDisplay(Display display, XmlWriterElementHelper xml)
        {
            xml.WriteElementValue(nameof(Display.StartInFullscreenMode), display.StartInFullscreenMode);
            xml.WriteElementValue(nameof(Display.ColorMode), display.ColorMode);
            xml.WriteElementValue(nameof(Display.DpiScalingMethod), display.DpiScalingMethod);
            if (display.CustomDateFormat is not null)
                xml.WriteElementValue(nameof(Display.CustomDateFormat), display.CustomDateFormat);
            if (display.CustomTimeFormat is not null)
                xml.WriteElementValue(nameof(Display.CustomTimeFormat), display.CustomTimeFormat);
        }

        private static void SerializeUx(UX ux, XmlWriterElementHelper xml)
        {
            xml.WriteElementValue(nameof(UX.SearchRestrictedToCurrentCategory), ux.SearchRestrictedToCurrentCategory);
            xml.WriteElementValue(nameof(UX.AskConfirmDelete), ux.AskConfirmDelete);
            xml.WriteElementValue(nameof(UX.AskConfirmKarmaExpense), ux.AskConfirmKarmaExpense);
            xml.WriteElementValue(nameof(UX.HideAvailabilityCreation), ux.HideAvailabilityCreation);
            xml.WriteElementValue(nameof(UX.AllowEasterEggs), ux.AllowEasterEggs);
            xml.WriteElementValue(nameof(UX.HideMasterIndex), ux.HideMasterIndex);
            xml.WriteElementValue(nameof(UX.HideCharacterRoster), ux.HideCharacterRoster);
            xml.WriteElementValue(nameof(UX.SingleDiceRoller), ux.SingleDiceRoller);
            xml.WriteElementValue(nameof(UX.AllowScrollIncrement), ux.AllowScrollIncrement);
            xml.WriteElementValue(nameof(UX.AllowScrollTabSwitch), ux.AllowScrollTabSwitch);
            xml.WriteElementValue(nameof(UX.AllowSkillDiceRolling), ux.AllowSkillDiceRolling);
            xml.WriteElementValue(nameof(UX.SetTimeWithDate), ux.SetTimeWithDate);
            xml.WriteElementValue(nameof(UX.DefaultMasterIndexSettingsFile), ux.DefaultMasterIndexSettingsFile);
        }

        private static void SerializeSaving(Saving saving, XmlWriterElementHelper xml)
        {
            xml.WriteElementValue(nameof(Saving.SaveCompressionLevel), saving.SaveCompressionLevel);
            xml.WriteElementValue(nameof(Saving.ImageCompressionLevel), saving.ImageCompressionLevel);
        }

        private static void SerializeLogging(Logging logging, XmlWriterElementHelper xml)
        {
            xml.WriteElementValue(nameof(Logging.LogLevel), logging.LogLevel);
            xml.WriteElementValue(nameof(Logging.LoggingResetCountdown), logging.LoggingResetCountdown);
        }

        private static void SerializeCharacter(Character character, XmlWriterElementHelper xml)
        {
            if (character.RosterPath is not null)
                xml.WriteElementValue(nameof(Character.RosterPath), character.RosterPath.FullName);
            xml.WriteElementValue(nameof(Character.CreateBackupOnCareer), character.CreateBackupOnCareer);
            xml.WriteElementValue(nameof(Character.DefaultSettingsFile), character.DefaultSettingsFile);
            xml.WriteElementValue(nameof(Character.LiveRefresh), character.LiveRefresh);
            xml.WriteElementValue(nameof(Character.EnableLifeModules), character.EnableLifeModules);
        }

        private static void SerializeCharacterList(List<FileInfo> MostRecentlyUsed, XmlWriterElementHelper xml)
        {
            foreach (FileInfo file in MostRecentlyUsed)
            {
                xml.WriteElementValue("File", file.FullName);
            }
        }
        private static void SerializeSourcebook(List<Sourcebook> Sourcebooks, XmlWriterElementHelper xml)
        {
            foreach (Sourcebook sb in Sourcebooks)
            {
                using (xml.OpenElement(nameof(Sourcebook)))
                {
                    xml.WriteElementValue(nameof(Sourcebook.Key), sb.Key);
                    xml.WriteElementValue(nameof(Sourcebook.Path), sb.Path.FullName);
                    xml.WriteElementValue(nameof(Sourcebook.PageOffset), sb.PageOffset);
                }
            }
        }
    }

    public static class LegacySettingsManager
    {
        public static GlobalSettings? LoadLegacyRegistrySettings()
        {
            if (!OperatingSystem.IsWindows())
            {
                // todo: diagnostics?
                return null;
            }
            using RegistryKey? baseKey = Registry.CurrentUser.OpenSubKey("Software\\Chummer5");
            if (baseKey is null)
                return null;
            GlobalSettings def = GlobalSettings.DefaultSettings;
            Update update = new Update(
                ShouldAutoUpdate: baseKey.LoadValueOrDefault("autoupdate", def.Update.ShouldAutoUpdate),
                PreferNightly: baseKey.LoadValueOrDefault("prefernightlybuilds", def.Update.PreferNightly)
            );

            CustomData cd = new CustomData(
                AllowLiveUpdates: baseKey.LoadValueOrDefault("livecustomdata", def.CustomData.AllowLiveUpdates),
                CustomDataDirectories: LoadCustomDataDirectories(baseKey.OpenSubKey("CustomDataDirectory"), def.CustomData.CustomDataDirectories)
            );

            Pdf pdf = new Pdf(
                ApplicationPath: baseKey.LoadValueOrDefault("pdfapppath", def.Pdf.ApplicationPath),
                ParametersStyle: ParsePdfParameters(baseKey.GetValue("pdfparameters"), def.Pdf.ParametersStyle),
                InsertPdfNotes: baseKey.LoadValueOrDefault("insertpdfnotesifavailable", def.Pdf.InsertPdfNotes)
            );

            Print print = new Print(
                PrintToFileFirst: baseKey.LoadValueOrDefault("printtofilefirst", def.Print.PrintToFileFirst),
                PrintZeroRatingSkills: baseKey.LoadValueOrDefault("printzeroratingskills", def.Print.PrintZeroRatingSkills),
                PrintExpenses: ParsePrintExpenses(baseKey, def.Print.PrintExpenses),
                PrintNotes: baseKey.LoadValueOrDefault("printnotes", def.Print.PrintNotes),
                DefaultPrintSheet: baseKey.LoadValueOrDefault("defaultsheet", def.Print.DefaultPrintSheet)
            );

            Display display = new Display(
                StartInFullscreenMode: baseKey.LoadValueOrDefault("startupfullscreen", def.Display.StartInFullscreenMode),
                ColorMode: baseKey.LoadEnumOrDefault("colormode", def.Display.ColorMode),
                DpiScalingMethod: baseKey.LoadEnumOrDefault("dpiscalingmethod", def.Display.DpiScalingMethod),
                CustomDateFormat: LoadCustomFormat(baseKey, "customdateformat", def.Display.CustomDateFormat),
                CustomTimeFormat: LoadCustomFormat(baseKey, "customtimeformat", def.Display.CustomTimeFormat)
            );

            UX ux = new UX(
                SearchRestrictedToCurrentCategory: baseKey.LoadValueOrDefault("searchincategoryonly", def.UX.SearchRestrictedToCurrentCategory),
                AskConfirmDelete: baseKey.LoadValueOrDefault("confirmdelete", def.UX.AskConfirmDelete),
                AskConfirmKarmaExpense: baseKey.LoadValueOrDefault("confirmkarmaexpense", def.UX.AskConfirmKarmaExpense),
                HideAvailabilityCreation: baseKey.LoadValueOrDefault("hideitemsoveravaillimit", def.UX.HideAvailabilityCreation),
                AllowEasterEggs: baseKey.LoadValueOrDefault("alloweastereggs", def.UX.AllowEasterEggs),
                HideMasterIndex: baseKey.LoadValueOrDefault("hidemasterindex", def.UX.HideMasterIndex),
                HideCharacterRoster: baseKey.LoadValueOrDefault("hidecharacterroster", def.UX.HideCharacterRoster),
                SingleDiceRoller: baseKey.LoadValueOrDefault("singlediceroller", def.UX.SingleDiceRoller),
                AllowScrollIncrement: baseKey.LoadValueOrDefault("allowhoverincrement", def.UX.AllowScrollIncrement),
                AllowScrollTabSwitch: baseKey.LoadValueOrDefault("switchtabsonhoverscroll", def.UX.AllowScrollTabSwitch),
                AllowSkillDiceRolling: baseKey.LoadValueOrDefault("allowskilldicerolling", def.UX.AllowSkillDiceRolling),
                SetTimeWithDate: baseKey.LoadValueOrDefault("datesincludetime", def.UX.SetTimeWithDate),
                DefaultMasterIndexSettingsFile: baseKey.LoadValueOrDefault("defaultmasterindexsetting", def.UX.DefaultMasterIndexSettingsFile)
            );

            Saving saving = new Saving(
                SaveCompressionLevel: baseKey.LoadEnumOrDefault("chum5lzcompressionlevel", def.Saving.SaveCompressionLevel),
                ImageCompressionLevel: ParseImageCompression(baseKey.GetValue("savedimagequality"), def.Saving.ImageCompressionLevel)
            );

            Logging logging = new Logging(
                LogLevel: ParseLogLevel(baseKey.GetValue("useloggingApplicationInsights"), def.Logging.LogLevel),
                LoggingResetCountdown: baseKey.LoadValueOrDefault("useloggingApplicationInsightsResetCounter", def.Logging.LoggingResetCountdown)
            );

            Character character = new Character(
                RosterPath: baseKey.LoadValueOrDefault("characterrosterpath", def.Character.RosterPath),
                CreateBackupOnCareer: baseKey.LoadValueOrDefault("createbackuponcareer", def.Character.CreateBackupOnCareer),
                DefaultSettingsFile: baseKey.LoadValueOrDefault("defaultcharactersetting", def.Character.DefaultSettingsFile),
                LiveRefresh: baseKey.LoadValueOrDefault("liveupdatecleancharacterfiles", def.Character.LiveRefresh),
                EnableLifeModules: baseKey.LoadValueOrDefault("lifemodule", def.Character.EnableLifeModules)
            );

            List<FileInfo> mru = GetCharacterList(baseKey, "mru");
            List<FileInfo> faves = GetCharacterList(baseKey, "stickymru");

            List<Sourcebook> sourcebooks = GetSourcebooks(baseKey.OpenSubKey("Sourcebook"));

            CultureInfo lang = ParseLanguage(baseKey.LoadValueOrDefault<string>("language", null), def.Language);

            return new GlobalSettings(
                update, cd, pdf, print, display, ux, saving, logging, character,
                lang, mru, faves, sourcebooks
            );
            static CultureInfo ParseLanguage(string? value, CultureInfo defaultValue)
            {
                if (value is null)
                    return defaultValue;
                try
                {
                    return value switch
                    {
                        "en-us2" => CultureInfo.GetCultureInfo("en-us"),
                        "de" => CultureInfo.GetCultureInfo("de-de"),
                        "fr" => CultureInfo.GetCultureInfo("fr-fr"),
                        "jp" => CultureInfo.GetCultureInfo("ja-jp"),
                        "zh" => CultureInfo.GetCultureInfo("zh-cn"),
                        _ => CultureInfo.GetCultureInfo(value)
                    };
                }
                catch (CultureNotFoundException)
                {
                    return defaultValue;
                }
            }
            static List<Sourcebook> GetSourcebooks(RegistryKey? key)
            {
                if (key is null)
                    return new List<Sourcebook>();
                List<Sourcebook> sb = new List<Sourcebook>();
                foreach (string bookkey in key.GetValueNames())
                {
                    string value = key.GetValue(bookkey)!.ToString()!;
                    var segments = value.Split('|');
                    if (segments.Length != 2)
                        continue; // malformed
                    if (!int.TryParse(segments.Last(), out int offset))
                        continue; // also malformed
                    if (string.IsNullOrWhiteSpace(segments.First()))
                        continue; // no path actually saved
                    FileInfo path = new FileInfo(segments.First());
                    sb.Add(new Sourcebook(bookkey, path, offset));
                }
                return sb;
            }
            static List<FileInfo> GetCharacterList(RegistryKey key, string prefix)
            {
                List<FileInfo> files = new List<FileInfo>();
                const int Size = 10;
                foreach (int i in Enumerable.Range(1, Size))
                {
                    // i'm like 90% sure the culture won't matter with what we're dealing with
                    // but that's not 100% sure!
                    string? filename = key.LoadValueOrDefault<string>(string
                        .Create(CultureInfo.InvariantCulture, $"{prefix}{i}"), null);
                    if (filename is not null)
                        files.Add(new FileInfo(filename));
                }
                return files;
            }
            static LogLevel ParseLogLevel(object? param, LogLevel defaultValue)
            {
                string? value = param?.ToString();
                if (value is null)
                    return defaultValue;
                if (value is "False")
                    return LogLevel.NotSet;
                if (value is "True")
                    return LogLevel.Info;
                if (Enum.TryParse(value, out LogLevel result))
                    return result;
                return defaultValue;
            }
            static ImageCompression ParseImageCompression(object? param, ImageCompression defaultValue)
            {
                string? value = param?.ToString();
                if (value is null || !int.TryParse(value, out int intValue))
                    return defaultValue;
                // just arbitary numbers to match it to roughly the closest setting
                return intValue switch
                {
                    int.MaxValue => ImageCompression.Png,
                    >= 85 => ImageCompression.JpegHigh,
                    >= 75 => ImageCompression.JpegMedium,
                    >= 40 => ImageCompression.JpegLow,
                    >= 20 => ImageCompression.JpegExtraLow,
                    >= 0 => ImageCompression.JpegExtraExtraLow,
                    < 0 => ImageCompression.JpegAutomatic,
                };

            }
            static string? LoadCustomFormat(RegistryKey baseKey, string key, string? defaultValue)
            {
                bool useCustom = baseKey.LoadValueOrDefault("usecustomdatetime", false);
                if (!useCustom) return defaultValue;
                return baseKey.LoadValueOrDefault(key, defaultValue);
            }
            static IReadOnlyList<DirectoryInfo> LoadCustomDataDirectories(RegistryKey? key, IReadOnlyList<DirectoryInfo> defaultValue)
            {
                // The old format stores all the custom data directories literally
                // We want a new one where we store the directory where the subdirectories are searched for custom data
                // Exclude approot/customdata because that's assumed to always be searched

                // It might not be safe to parse them and assume the containing folder, so we won't
                // But here is a good extension point for diagnostics, if we bother to
                return defaultValue;
            }
            static PdfParametersStyle ParsePdfParameters(object? param, PdfParametersStyle defaultValue)
            {
                string? value = param?.ToString();
                if (value is null)
                    return defaultValue;
                return value switch
                {
                    "\"file://{absolutepath}#page={page}\"" => PdfParametersStyle.WebBrowserStyle,
                    "/A \"page={page}\" \"{localpath}\"" => PdfParametersStyle.AcrobatStyle,
                    "/A /N \"page={page}\" \"{localpath}\"" => PdfParametersStyle.AcrobatStyleNewInstance,
                    "-p {page} \"{localpath}" => PdfParametersStyle.UnixStyle,
                    "-reuse-instance -page {page} \"{localpath}\"" => PdfParametersStyle.SumatraStyleReuseInstance,
                    "-page {page} \"{localpath}\"" => PdfParametersStyle.SumatraStyle,
                    _ => defaultValue, // could use some diagnostics
                };
            }
            static PrintExpenses ParsePrintExpenses(RegistryKey key, PrintExpenses defaultValue)
            {
                bool peFound = key.TryLoadValue("printexpenses", out bool printexpenses);
                if (!peFound) return defaultValue;
                if (!printexpenses) return PrintExpenses.NoPrint;
                bool pfeFound = key.TryLoadValue("printfreeexpenses", out bool printfreeexpenses);
                if (!pfeFound) return PrintExpenses.PrintValueExpenses;
                if (!printfreeexpenses) return PrintExpenses.PrintValueExpenses;
                return PrintExpenses.PrintAllExpenses;
            }
        }

        [return: NotNullIfNotNull(nameof(defaultValue))]
        private static T? LoadValueOrDefault<T>(this RegistryKey baseKey, string key, T? defaultValue)
            where T : IParsable<T>
        {
            Debug.Assert(OperatingSystem.IsWindows());
            object? value = baseKey.GetValue(key);
            if (value is not null && T.TryParse(value.ToString(), CultureInfo.InvariantCulture, out T? parsedValue))
                return parsedValue;
            return defaultValue;
        }

        private static T LoadEnumOrDefault<T>(this RegistryKey baseKey, string key, T defaultValue)
            where T : struct, Enum
        {
            Debug.Assert(OperatingSystem.IsWindows());
            object? value = baseKey.GetValue(key);
            if (value is not null && Enum.TryParse<T>(value.ToString(), true, out T parsedValue))
                return parsedValue;
            return defaultValue;
        }

        [return: NotNullIfNotNull(nameof(defaultValue))]
        private static FileInfo? LoadValueOrDefault(this RegistryKey baseKey, string key, FileInfo? defaultValue)
        {
            Debug.Assert(OperatingSystem.IsWindows());
            object? value = baseKey.GetValue(key);
            if (value is not null)
            {
                string? vstr = value.ToString();
                if (vstr is not null)
                    return new FileInfo(vstr);
            }
            return defaultValue;
        }

        [return: NotNullIfNotNull(nameof(defaultValue))]
        private static DirectoryInfo? LoadValueOrDefault(this RegistryKey baseKey, string key, DirectoryInfo? defaultValue)
        {
            Debug.Assert(OperatingSystem.IsWindows());
            object? value = baseKey.GetValue(key);
            if (value is not null)
            {
                string? vstr = value.ToString();
                if (vstr is not null)
                    return new DirectoryInfo(vstr);
            }
            return defaultValue;
        }

        private static bool TryLoadValue<T>(this RegistryKey baseKey, string key, [NotNullWhen(true)] out T? value)
            where T : IParsable<T>
        {
            Debug.Assert(OperatingSystem.IsWindows());
            object? objval = baseKey.GetValue(key);
            if (objval is not null)
                return T.TryParse(objval.ToString(), null, out value);
            value = default;
            return false;
        }
    }
}
