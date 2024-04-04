using Chummer.Api.Enums;
using Chummer.Api.Models.GlobalSettings;
using Microsoft.Win32;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Chummer.Api
{
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

            CustomData cd;
            using (var cdd = baseKey.OpenSubKey("CustomDataDirectory"))
            {
                cd = new CustomData(
                    AllowLiveUpdates: baseKey.LoadValueOrDefault("livecustomdata", def.CustomData.AllowLiveUpdates),
                    CustomDataDirectories: LoadCustomDataDirectories(cdd, def.CustomData.CustomDataDirectories)
                );
            }

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
                HideItemsOverAvailabilityLimitInCreate: baseKey.LoadValueOrDefault("hideitemsoveravaillimit", def.UX.HideItemsOverAvailabilityLimitInCreate),
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
                ImageCompressionLevel: ParseImageCompression(baseKey.GetValue("savedimagequality"), def.Saving.ImageCompressionLevel),
                LastMugshotFolder: baseKey.LoadValueOrDefault("recentimagefolder", def.Saving.LastMugshotFolder)
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

            List<Sourcebook> sourcebooks;
            using (var sbkey = baseKey.OpenSubKey("Sourcebook"))
            {
                sourcebooks = GetSourcebooks(sbkey);
            }

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
