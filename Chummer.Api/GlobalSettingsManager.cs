using Chummer.Api.Models.GlobalSettings;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Chummer.Api
{
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
                HideItemsOverAvailabilityLimitInCreate: ux.ChildValue(nameof(UX.HideItemsOverAvailabilityLimitInCreate), def.HideItemsOverAvailabilityLimitInCreate),
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
                ImageCompressionLevel: saving.ChildValueEnum(nameof(Saving.ImageCompressionLevel), def.ImageCompressionLevel),
                LastMugshotFolder: saving.ChildValue(nameof(Saving.LastMugshotFolder), def.LastMugshotFolder)
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
            xml.WriteElementValue(nameof(UX.HideItemsOverAvailabilityLimitInCreate), ux.HideItemsOverAvailabilityLimitInCreate);
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
            if (saving.LastMugshotFolder is not null)
                xml.WriteElementValue(nameof(Saving.LastMugshotFolder), saving.LastMugshotFolder.FullName);
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
}
