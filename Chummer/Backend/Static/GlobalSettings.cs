/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using iText.Kernel.Pdf;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Win32;
using NLog;
using Xoshiro.PRNG64;
using Chummer.Api;
using System.Diagnostics;
using System.Windows.Controls;
using Chummer.Api.Enums;
using Chummer.Api.Models.GlobalSettings;
using Image = System.Drawing.Image;

namespace Chummer
{
    public enum ClipboardContentType
    {
        None = 0,
        Armor,
        ArmorMod,
        Cyberware,
        Gear,
        Lifestyle,
        Vehicle,
        Weapon,
        WeaponAccessory
    }

    public enum UseAILogging
    {
        OnlyLocal = 0,
        OnlyMetric,
        Crashes,
        NotSet,
        Info,
        Trace
    }

    public enum ColorMode
    {
        Automatic = 0,
        Light,
        Dark
    }

    public enum DpiScalingMethod
    {
        None = 0,
        Zoom,       // System
        Rescale,    // PerMonitor/PerMonitorV2
        SmartZoom   // System (Enhanced)
    }

    public sealed class SourcebookInfo : IDisposable
    {
        private static readonly Lazy<Logger> s_ObjLogger = new Lazy<Logger>(LogManager.GetCurrentClassLogger);
        private static Logger Log => s_ObjLogger.Value;
        private string _strPath = string.Empty;
        private PdfReader _objPdfReader;
        private PdfDocument _objPdfDocument;

        public SourcebookInfo()
        {
        }

        /// <summary>
        /// Special constructor used when we have already created a PdfReader and PdfDocument assigned to a file representing this SourcebookInfo
        /// </summary>
        /// <param name="strPath">Path to the file.</param>
        /// <param name="objPdfReader">PdfReader object associated with the file.</param>
        /// <param name="objPdfDocument">PdfDocument object associated with the file.</param>
        /// <exception cref="ArgumentException"><paramref name="objPdfDocument"/>'s associated reader is not the same value as<paramref name="objPdfReader"/>.</exception>
        [CLSCompliant(false)]
        public SourcebookInfo(string strPath, PdfReader objPdfReader, PdfDocument objPdfDocument)
        {
            if (string.IsNullOrEmpty(strPath))
                throw new ArgumentNullException(nameof(strPath));
            _strPath = strPath;
            _objPdfReader = objPdfReader ?? throw new ArgumentNullException(nameof(objPdfReader));
            if (objPdfDocument?.GetReader() != objPdfReader)
                throw new ArgumentException("objPdfDocument reader is different from objPdfReader", nameof(objPdfDocument));
            _objPdfDocument = objPdfDocument;
        }

        #region Properties

        public string Code { get; set; } = string.Empty;

        public string Path
        {
            get => _strPath;
            set
            {
                if (Interlocked.Exchange(ref _strPath, value) == value)
                    return;
                Interlocked.Exchange(ref _objPdfDocument, null)?.Close();
                Interlocked.Exchange(ref _objPdfReader, null)?.Close();
            }
        }

        public int Offset { get; set; }

        internal PdfDocument CachedPdfDocument
        {
            get
            {
                if (_objPdfDocument == null)
                {
                    string strPath = new Uri(Path).LocalPath;
                    if (File.Exists(strPath))
                    {
                        try
                        {
                            PdfReader objReader = new PdfReader(strPath);
                            PdfDocument objReturn = new PdfDocument(objReader);
                            Interlocked.Exchange(ref _objPdfDocument, objReturn)?.Close();
                            Interlocked.Exchange(ref _objPdfReader, objReader)?.Close();
                            return objReturn;
                        }
                        catch (Exception e)
                        {
                            Log.Warn(e, $"Exception while loading {strPath}: " + e.Message);
                            Interlocked.Exchange(ref _objPdfDocument, null)?.Close();
                            Interlocked.Exchange(ref _objPdfReader, null)?.Close();
                        }
                    }
                }
                return _objPdfDocument;
            }
        }

        #region IDisposable Support

        private int _intIsDisposed; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (disposing && Interlocked.CompareExchange(ref _intIsDisposed, 1, 0) == 0)
            {
                _objPdfDocument?.Close();
                _objPdfReader?.Close();
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion IDisposable Support

        #endregion Properties
    }

    /// <summary>
    /// Global Settings. A single instance class since Settings are common for all characters, reduces execution time and memory usage.
    /// </summary>
    public static class GlobalSettings
    {
        public static string ErrorMessage { get; }

        public static event EventHandler<TextEventArgs> MruChanged;

        public static event PropertyChangedEventHandler ClipboardChanged;

        public const int MaxMruSize = 10;
        private static readonly MostRecentlyUsedCollection<string> s_LstMostRecentlyUsedCharacters = new MostRecentlyUsedCollection<string>(MaxMruSize);
        private static readonly MostRecentlyUsedCollection<string> s_LstFavoriteCharacters = new MostRecentlyUsedCollection<string>(MaxMruSize);

        public const string DefaultLanguage = "en-us";
        public const string DefaultCharacterSheetDefaultValue = "Shadowrun 5 (Skills grouped by Rating greater 0)";
        public const string DefaultCharacterSettingDefaultValue = "223a11ff-80e0-428b-89a9-6ef1c243b8b6"; // GUID for built-in Standard option
        public const string DefaultMasterIndexSettingDefaultValue = "67e25032-2a4e-42ca-97fa-69f7f608236c"; // GUID for built-in Full House option
        public const DpiScalingMethod DefaultDpiScalingMethod = DpiScalingMethod.Zoom;
        public const LzmaHelper.ChummerCompressionPreset DefaultChum5lzCompressionLevel
            = LzmaHelper.ChummerCompressionPreset.Balanced;

        public const int MaxStackLimit = 1024;
        public static ThreadSafeCachedRandom RandomGenerator { get; } = new ThreadSafeCachedRandom(new XoRoShiRo128starstar(), true);

        public static ConcurrentDictionary<string, bool> PluginsEnabledDic => new ConcurrentDictionary<string, bool>();


        private static readonly HashSet<CustomDataDirectoryInfo> s_SetCustomDataDirectoryInfos = new HashSet<CustomDataDirectoryInfo>();

        /// <summary>
        /// Load a Bool Option from the Registry.
        /// </summary>
        public static bool LoadBoolFromRegistry(ref bool blnStorage, string strBoolName, string strSubKey = "", bool blnDeleteAfterFetch = false)
        {
            RegistryKey objKey = string.IsNullOrWhiteSpace(strSubKey)
                ? BaseKey
                : BaseKey.OpenSubKey(strSubKey);
            if (objKey == null)
                return false;
            try
            {
                object objRegistryResult = objKey.GetValue(strBoolName);
                if (objRegistryResult != null)
                {
                    if (bool.TryParse(objRegistryResult.ToString(), out bool blnTemp))
                        blnStorage = blnTemp;
                    if (blnDeleteAfterFetch)
                        objKey.DeleteValue(strBoolName);
                    return true;
        }
            }
            finally
            {
                if (objKey != BaseKey)
                    objKey.Close();
            }

            return false;
        }

        /// <summary>
        /// Load an String Option from the Registry.
        /// </summary>
        public static bool LoadStringFromRegistry(ref string strStorage, string strStringName, string strSubKey = "", bool blnDeleteAfterFetch = false)
        {
            RegistryKey objKey = string.IsNullOrWhiteSpace(strSubKey)
                ? BaseKey
                : BaseKey.OpenSubKey(strSubKey);
            if (objKey == null)
                return false;
            try
            {
                object objRegistryResult = objKey.GetValue(strStringName);
                if (objRegistryResult != null)
                {
                    strStorage = objRegistryResult.ToString();
                    if (blnDeleteAfterFetch)
                        objKey.DeleteValue(strStringName);
                    return true;
        }
            }
            finally
            {
                if (objKey != BaseKey)
                    objKey.Close();
            }

            return false;
        }

        private static Api.Models.GlobalSettings.GlobalSettings settings;
        private static readonly GlobalSettingsManager gsm;
        private static readonly FileInfo settingsFile;
        private static readonly ChummerDataLoader cdl;

        private static RegistryKey BaseKey;

        static GlobalSettings()
        {
            if (Utils.IsDesignerMode)
            {
                settings = Api.Models.GlobalSettings.GlobalSettings.DefaultSettings;
                return;
            }

            cdl = new ChummerDataLoader(
                new XmlFileProvider(new DirectoryInfo(Utils.GetDataFolderPath))
            );

            BaseKey = Registry.CurrentUser.CreateSubKey("Software\\Chummer5", true);

            settingsFile = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
                "Chummer5", "GlobalSettings.xml"));
            gsm = new GlobalSettingsManager();
            if (!settingsFile.Exists)
            {
                settings = LegacySettingsManager.LoadLegacyRegistrySettings();
                if (settings is not null) // succeeded at loading legacy settings
                {
                    if (!settingsFile.Directory.Exists)
                        settingsFile.Directory.Create();
                    using (FileStream fs = settingsFile.Open(FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        gsm.SerializeGlobalSettings(settings, fs);
                    }
                    Program.ShowScrollableMessageBox(LanguageManager.GetString("Message_ImportedLegacySettings"));
                }
                else
                {
                    settings = Api.Models.GlobalSettings.GlobalSettings.DefaultSettings;
                }
            }
            else
            {
                using FileStream fs = settingsFile.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                settings = gsm.LoadGlobalSettings(fs);
            }

            if (settings.Logging.LoggingResetCountdown > 0)
            {
                settings = settings with
                {
                    Logging = settings.Logging with
                    {
                        LoggingResetCountdown = settings.Logging.LoggingResetCountdown - 1
                    }
                };
            }

            Utils.SetupWebBrowserRegistryKeys();

            // Add in default customdata path and generate custom datas
            var cd = new List<DirectoryInfo>(settings.CustomData.CustomDataDirectories)
            {
                new DirectoryInfo(Path.Combine(Utils.GetStartupPath, "customdata"))
            };

            s_SetCustomDataDirectoryInfos = cd.SelectMany(EnumerateCustomData).ToHashSet();

            static IEnumerable<CustomDataDirectoryInfo> EnumerateCustomData(DirectoryInfo dir)
            {
                if (dir.Exists)
                {
                    foreach (var subdir in dir.EnumerateDirectories())
                    {
                        var info = new CustomDataDirectoryInfo(
                            subdir.Name,
                            subdir.FullName
                        );
                        if (info.XmlException != null)
                        {
                            Program.ShowScrollableMessageBox(
                                string.Format(CultureInfo, LanguageManager.GetString("Message_FailedLoad"),
                                    info.XmlException.Message),
                                string.Format(CultureInfo,
                                    LanguageManager.GetString("MessageTitle_FailedLoad") +
                                    LanguageManager.GetString("String_Space") + info.Name +
                                    Path.DirectorySeparatorChar + "manifest.xml"), MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                        yield return info;
                    }
                }
            }

            XmlManager.RebuildDataDirectoryInfo(s_SetCustomDataDirectoryInfos);

            s_LstFavoriteCharacters.AddRange(
                settings.FavoriteCharacters.Where(ch => ch.Exists).Select(m => m.FullName).Distinct()
            );
            
            s_LstFavoriteCharacters.Sort();
            s_LstFavoriteCharacters.CollectionChangedAsync += LstFavoritedCharactersOnCollectionChanged;

            s_LstMostRecentlyUsedCharacters.AddRange(
                settings.MostRecentlyUsed.Where(ch => ch.Exists).Select(m => m.FullName).Distinct()
            );
            s_LstMostRecentlyUsedCharacters.CollectionChangedAsync += LstMostRecentlyUsedCharactersOnCollectionChanged;
        }

        public static async Task SaveOptionsToRegistry(CancellationToken token = default)
        {
            using FileStream fs = settingsFile.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            gsm.SerializeGlobalSettings(settings, fs);
        }

        /// <summary>
        /// Whether to create backups of characters before moving them to career mode. If true, a separate save file is created before marking the current character as created.
        /// </summary>
        public static bool CreateBackupOnCareer
        {
            get => settings.Character.CreateBackupOnCareer;
            set => settings = settings with
            {
                Character = settings.Character with
                {
                    CreateBackupOnCareer = value
                }
            };
        }

        /// <summary>
        /// Should the Plugins-Directory be loaded and the tabPlugins be shown?
        /// </summary>
        public static bool PluginsEnabled
        {
            get => false;
            set
            {
                // intentionally do nothing
            }
        }

        /// <summary>
        /// Should Chummer present Easter Eggs to the user?
        /// </summary>
        public static bool AllowEasterEggs
        {
            get => settings.UX.AllowEasterEggs;
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    AllowEasterEggs = value
                }
            };
        }

        /// <summary>
        /// Whether the Master Index should be shown. If true, prevents the roster from being removed or hidden.
        /// </summary>
        public static bool HideMasterIndex
        {
            get => settings.UX.HideMasterIndex;
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    HideMasterIndex = value
                }
            };
        }

        /// <summary>
        /// Whether the Character Roster should be shown. If true, prevents the roster from being removed or hidden.
        /// </summary>
        public static bool HideCharacterRoster
        {
            get => settings.UX.HideCharacterRoster;
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    HideCharacterRoster = value
                }
            };
        }

        /// <summary>
        /// DPI Scaling method to use
        /// </summary>
        public static DpiScalingMethod DpiScalingMethodSetting
        {
            // dirty hack until we remove the shim
            // could write a faster version, but it doesn't need to be fast
            get => Enum.Parse<DpiScalingMethod>(settings.Display.DpiScalingMethod.ToString());
            set => settings = settings with
            {
                Display = settings.Display with
                {
                    DpiScalingMethod = Enum.Parse<Api.Enums.DpiScalingMethod>(value.ToString())
                }
            };
        }

        /// <summary>
        /// Whether Automatic Updates are enabled.
        /// </summary>
        public static bool AutomaticUpdate
        {
            get => settings.Update.ShouldAutoUpdate;
            set => settings = settings with
            {
                Update = settings.Update with
                {
                    ShouldAutoUpdate = value
                }
            };
        }

        /// <summary>
        /// Whether live updates from the customdata directory are allowed.
        /// </summary>
        public static bool LiveCustomData
        {
            get => settings.CustomData.AllowLiveUpdates;
            set => settings = settings with
            {
                CustomData = settings.CustomData with
                {
                    AllowLiveUpdates = value
                }
            };
        }

        public static bool LiveUpdateCleanCharacterFiles
        {
            get => settings.Character.LiveRefresh;
            set => settings = settings with
            {
                Character = settings.Character with
                {
                    LiveRefresh = value
                }
            };
        }

        public static bool LifeModuleEnabled
        {
            get => settings.Character.EnableLifeModules;
            set => settings = settings with
            {
                Character = settings.Character with
                {
                    EnableLifeModules = value
                }
            };
        }

        /// <summary>
        /// Whether confirmation messages are shown when deleting an object.
        /// </summary>
        public static bool ConfirmDelete
        {
            get => settings.UX.AskConfirmDelete;
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    AskConfirmDelete = value
                }
            };
        }

        /// <summary>
        /// Whether confirmation messages are shown for Karma Expenses.
        /// </summary>
        public static bool ConfirmKarmaExpense
        {
            get => settings.UX.AskConfirmKarmaExpense;
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    AskConfirmKarmaExpense = value
                }
            };
        }

        /// <summary>
        /// Whether items that exceed the Availability Limit should be shown in Create Mode.
        /// </summary>
        public static bool HideItemsOverAvailLimit
        {
            get => settings.UX.HideItemsOverAvailabilityLimitInCreate;
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    HideItemsOverAvailabilityLimitInCreate = value
                }
            };
        }

        /// <summary>
        /// Whether numeric updowns can increment values of numericupdown controls by hovering over the control.
        /// </summary>
        public static bool AllowHoverIncrement
        {
            get => settings.UX.AllowScrollIncrement;
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    AllowScrollIncrement = value
                }
            };
        }

        /// <summary>
        /// Whether scrolling the mouse wheel while hovering over tab page labels switches tabs
        /// </summary>
        public static bool SwitchTabsOnHoverScroll
        {
            get => settings.UX.AllowScrollTabSwitch;
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    AllowScrollTabSwitch = value
                }
            };
        }

        /// <summary>
        /// Whether searching in a selection form will limit itself to the current Category that's selected.
        /// </summary>
        public static bool SearchInCategoryOnly
        {
            get => settings.UX.SearchRestrictedToCurrentCategory;
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    SearchRestrictedToCurrentCategory = value
                }
            };
        }

        public static NumericUpDownEx.InterceptMouseWheelMode InterceptMode => AllowHoverIncrement
            ? NumericUpDownEx.InterceptMouseWheelMode.WhenMouseOver
            : NumericUpDownEx.InterceptMouseWheelMode.WhenFocus;

        /// <summary>
        /// Whether dice rolling is allowed for Skills.
        /// </summary>
        public static bool AllowSkillDiceRolling
        {
            get => settings.UX.AllowSkillDiceRolling;
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    AllowSkillDiceRolling = value
                }
            };
        }

        /// <summary>
        /// Whether the app should use logging.
        /// </summary>
        public static bool UseLogging
        {
            get => settings.Logging.LogLevel != Api.Enums.LogLevel.NoLogging;
            set
            {
                settings = settings with
                {
                    Logging = settings.Logging with
                    {
                        LogLevel = value ? Api.Enums.LogLevel.OnlyMetric : Api.Enums.LogLevel.NoLogging
                    }
                };
                if (value)
                {
                    if (!LogManager.IsLoggingEnabled())
                        LogManager.ResumeLogging();
                }
                else if (LogManager.IsLoggingEnabled())
                    LogManager.SuspendLogging();
            }
        }

        /// <summary>
        /// Should actually the set LoggingLevel be used or only a more conservative one
        /// </summary>
        public static int UseLoggingResetCounter
        {
            get => (int)settings.Logging.LoggingResetCountdown;
            set => settings = settings with
            {
                Logging = settings.Logging with
                {
                    LoggingResetCountdown = (uint)value
                }
            };
        }

        /// <summary>
        /// What Logging Level are we "allowed" to use by the user. The actual used Level is the UseLoggingApplicationInsights and depends on
        /// nightly/stable and ResetLoggingCounter
        /// </summary>
        public static UseAILogging UseLoggingApplicationInsightsPreference
        {
            // dirty dirty hack
            get => UseLogging ? Enum.Parse<UseAILogging>(settings.Logging.LogLevel.ToString()) : UseAILogging.OnlyLocal;
            set
            {
                bool blnNewDisableTelemetry = value < UseAILogging.OnlyMetric;
                bool blnOldDisableTelemetry = UseLoggingApplicationInsightsPreference < UseAILogging.OnlyMetric;
                if (settings.Logging.LogLevel != Api.Enums.LogLevel.NoLogging)
                {
                    settings = settings with
                    {
                        Logging = settings.Logging with
                        {
                            LogLevel = Enum.Parse<Api.Enums.LogLevel>(value.ToString())
                        }
                    };
                }
                if (blnOldDisableTelemetry != blnNewDisableTelemetry && Program.ChummerTelemetryClient.IsValueCreated)
                {
                    // Sets up logging if the option is changed during runtime
                    TelemetryConfiguration objConfiguration = Program.ActiveTelemetryConfiguration;
                    if (objConfiguration != null)
                        objConfiguration.DisableTelemetry = blnNewDisableTelemetry;
                }
            }
        }

        /// <summary>
        /// Whether the app should use logging.
        /// </summary>
        public static UseAILogging UseLoggingApplicationInsights
        {
            get
            {
                if (UseLoggingApplicationInsightsPreference == UseAILogging.OnlyLocal)
                    return UseAILogging.OnlyLocal;

                if (UseLoggingResetCounter > 0)
                    return UseLoggingApplicationInsightsPreference;

                if (Utils.IsMilestoneVersion
                    //stable builds should not log more than metrics
                    && UseLoggingApplicationInsightsPreference > UseAILogging.OnlyMetric)
                    return UseAILogging.OnlyMetric;

                return UseLoggingApplicationInsightsPreference;
            }
            set => UseLoggingApplicationInsightsPreference = value;
        }

        /// <summary>
        /// Whether the program should be forced to use Light/Dark mode or to obey default color schemes automatically
        /// </summary>
        public static ColorMode ColorModeSetting => Enum.Parse<ColorMode>(settings.Display.ColorMode.ToString());

        public static Task SetColorModeSettingAsync(ColorMode value, CancellationToken token = default)
        {
            if (value == ColorModeSetting)
                return Task.CompletedTask;
            settings = settings with
            {
                Display = settings.Display with
                {   // lovin' these enum hacks
                    ColorMode = Enum.Parse<Api.Enums.ColorMode>(value.ToString())
                }
            };
            switch (value)
            {
                case ColorMode.Automatic:
                    return ColorManager.AutoApplyLightDarkModeAsync(token);

                case ColorMode.Light:
                    ColorManager.DisableAutoTimer();
                    return ColorManager.SetIsLightModeAsync(true, token);

                case ColorMode.Dark:
                    ColorManager.DisableAutoTimer();
                    return ColorManager.SetIsLightModeAsync(false, token);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Whether dates should include the time.
        /// </summary>
        public static bool DatesIncludeTime
        {
            get => settings.UX.SetTimeWithDate;
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    SetTimeWithDate = value
                }
            };
        }

        /// <summary>
        /// Whether printouts should be sent to a file before loading them in the browser. This is a fix for getting printing to work properly on Linux using Wine.
        /// </summary>
        public static bool PrintToFileFirst
        {
            get => settings.Print.PrintToFileFirst;
            set => settings = settings with
            {
                Print = settings.Print with
                {
                    PrintToFileFirst= value
                }
            };
        }

        /// <summary>
        /// Whether all Active Skills with a total score higher than 0 should be printed.
        /// </summary>
        public static bool PrintSkillsWithZeroRating
        {
            get => settings.Print.PrintZeroRatingSkills;
            set => settings = settings with
            {
                Print = settings.Print with
                {
                    PrintZeroRatingSkills = value
                }
            };
        }

        /// <summary>
        /// Whether the Karma and Nuyen Expenses should be printed on the character sheet.
        /// </summary>
        public static bool PrintExpenses
        {
            get => settings.Print.PrintExpenses is Api.Enums.PrintExpenses.PrintValueExpenses
                or Api.Enums.PrintExpenses.PrintAllExpenses;
            set
            {
                if (value && settings.Print.PrintExpenses is Api.Enums.PrintExpenses.PrintAllExpenses)
                    return;
                settings = settings with
                {
                    Print = settings.Print with
                    {
                        PrintExpenses = value ? Api.Enums.PrintExpenses.PrintValueExpenses : Api.Enums.PrintExpenses.NoPrint
                    }
                };
            }
        }

        /// <summary>
        /// Whether the Karma and Nuyen Expenses that have a cost of 0 should be printed on the character sheet.
        /// </summary>
        public static bool PrintFreeExpenses
        {
            get => settings.Print.PrintExpenses is Api.Enums.PrintExpenses.PrintAllExpenses;
            set
            {
                if (!PrintExpenses) return;
                settings = settings with
                {
                    Print = settings.Print with
                    {
                        PrintExpenses = value ? Api.Enums.PrintExpenses.PrintAllExpenses: Api.Enums.PrintExpenses.PrintValueExpenses
                    }
                };
            }
        }

        /// <summary>
        /// Whether Notes should be printed.
        /// </summary>
        public static bool PrintNotes
        {
            get => settings.Print.PrintNotes;
            set => settings = settings with
            {
                Print = settings.Print with
                {
                    PrintNotes = value
                }
            };
        }

        /// <summary>
        /// Whether to insert scraped text from PDFs into the Notes fields of newly added items.
        /// </summary>
        public static bool InsertPdfNotesIfAvailable
        {
            get => settings.Pdf.InsertPdfNotes;
            set => settings = settings with
            {
                Pdf = settings.Pdf with
                {
                    InsertPdfNotes = value
                }
            };
        }

        /// <summary>
        /// Which version of the Internet Explorer's rendering engine will be emulated for rendering the character view. Defaults to 11
        /// </summary>
        public static int EmulatedBrowserVersion
        {
            get => 11;
            set
            {
                // intentionally default
            }
        }

        /// <summary>
        /// Chummer's UI Language.
        /// </summary>
        public static string Language
        {
            get => settings.Language.Name;
            set
            {
                CultureInfo newLang;
                try
                {
                    newLang = CultureInfo.GetCultureInfo(value);
                }
                catch (CultureNotFoundException)
                {
                    newLang = SystemCultureInfo;
                }
                if (newLang.Name == settings.Language.Name)
                    return;
                settings = settings with { Language = newLang };
                // Set default cultures based on the currently set language
                CultureInfo.DefaultThreadCurrentCulture = settings.Language;
                CultureInfo.DefaultThreadCurrentUICulture = settings.Language;
                ChummerMainForm frmMain = Program.MainForm;
                if (frmMain == null)
                    return;
                try
                {
                    frmMain.TranslateWinForm();
                    IReadOnlyCollection<Form> lstToProcess = frmMain.OpenCharacterEditorForms;
                    if (lstToProcess != null)
                    {
                        foreach (Form frmLoop in lstToProcess)
                        {
                            frmLoop.TranslateWinForm();
                        }
                    }

                    lstToProcess = frmMain.OpenCharacterSheetViewers;
                    if (lstToProcess != null)
                    {
                        foreach (Form frmLoop in lstToProcess)
                        {
                            frmLoop.TranslateWinForm();
                        }
                    }

                    lstToProcess = frmMain.OpenCharacterExportForms;
                    if (lstToProcess != null)
                    {
                        foreach (Form frmLoop in lstToProcess)
                        {
                            frmLoop.TranslateWinForm();
                        }
                    }

                    frmMain.PrintMultipleCharactersForm?.TranslateWinForm();
                    frmMain.CharacterRoster?.TranslateWinForm();
                    frmMain.MasterIndex?.TranslateWinForm();
                    frmMain.RefreshAllTabTitles();
                }
                catch (ObjectDisposedException)
                {
                    //swallow this
                }
                catch (OperationCanceledException)
                {
                    //swallow this
                }
            }
        }

        public static async Task SetLanguageAsync(string value, CancellationToken token = default)
        {
            CultureInfo newLang;
            try
            {
                newLang = CultureInfo.GetCultureInfo(value);
            }
            catch (CultureNotFoundException)
            {
                newLang = SystemCultureInfo;
            }
            if (newLang.Name == settings.Language.Name)
                return;
            settings = settings with { Language = newLang };
            // Set default cultures based on the currently set language
            CultureInfo.DefaultThreadCurrentCulture = settings.Language;
            CultureInfo.DefaultThreadCurrentUICulture = settings.Language;
            ChummerMainForm frmMain = Program.MainForm;
            if (frmMain != null)
            {
                await frmMain.TranslateWinFormAsync(token: token).ConfigureAwait(false);
                IReadOnlyCollection<Form> lstToProcess = frmMain.OpenCharacterEditorForms;
                if (lstToProcess != null)
                {
                    foreach (Form frmLoop in lstToProcess)
                    {
                        await frmLoop.TranslateWinFormAsync(token: token).ConfigureAwait(false);
                    }
                }
                lstToProcess = frmMain.OpenCharacterSheetViewers;
                if (lstToProcess != null)
                {
                    foreach (Form frmLoop in lstToProcess)
                    {
                        await frmLoop.TranslateWinFormAsync(token: token).ConfigureAwait(false);
                    }
                }
                lstToProcess = frmMain.OpenCharacterExportForms;
                if (lstToProcess != null)
                {
                    foreach (Form frmLoop in lstToProcess)
                    {
                        await frmLoop.TranslateWinFormAsync(token: token).ConfigureAwait(false);
                    }
                }
                Form frmToProcess = frmMain.PrintMultipleCharactersForm;
                if (frmToProcess != null)
                    await frmToProcess.TranslateWinFormAsync(token: token).ConfigureAwait(false);
                frmToProcess = frmMain.CharacterRoster;
                if (frmToProcess != null)
                    await frmToProcess.TranslateWinFormAsync(token: token).ConfigureAwait(false);
                frmToProcess = frmMain.MasterIndex;
                if (frmToProcess != null)
                    await frmToProcess.TranslateWinFormAsync(token: token).ConfigureAwait(false);
                await frmMain.RefreshAllTabTitlesAsync(token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Whether the application should start in fullscreen mode.
        /// </summary>
        public static bool StartupFullscreen
        {
            get => settings.Display.StartInFullscreenMode;
            set => settings = settings with
            {
                Display = settings.Display with
                {
                    StartInFullscreenMode = value
                }
            };
        }

        /// <summary>
        /// Whether only a single instance of the Dice Roller should be allowed.
        /// </summary>
        public static bool SingleDiceRoller
        {
            get => settings.UX.SingleDiceRoller;
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    SingleDiceRoller = value
                }
            };
        }

        /// <summary>
        /// CultureInfo for number localization.
        /// </summary>
        public static CultureInfo CultureInfo => settings.Language;

        /// <summary>
        /// Invariant CultureInfo for saving and loading of numbers.
        /// </summary>
        public static CultureInfo InvariantCultureInfo => CultureInfo.InvariantCulture;

        /// <summary>
        /// CultureInfo of the user's current system.
        /// </summary>
        public static CultureInfo SystemCultureInfo => CultureInfo.CurrentCulture;

        private static XmlDocument _xmlClipboard = new XmlDocument { XmlResolver = null };
        private static readonly Lazy<Regex> s_RgxInvalidUnicodeCharsExpression = new Lazy<Regex>(() => new Regex(
            @"[\u0000-\u0008\u000B\u000C\u000E-\u001F]",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled));

        /// <summary>
        /// XmlReaderSettings that should be used when reading almost Xml readable.
        /// </summary>
        public static XmlReaderSettings SafeXmlReaderSettings { get; } = new XmlReaderSettings { XmlResolver = null, IgnoreComments = true, IgnoreWhitespace = true };

        /// <summary>
        /// XmlReaderSettings that should only be used if invalid characters are found.
        /// </summary>
        public static XmlReaderSettings UnSafeXmlReaderSettings { get; } = new XmlReaderSettings { XmlResolver = null, IgnoreComments = true, IgnoreWhitespace = true, CheckCharacters = false };

        /// <summary>
        /// Regex that indicates whether a given string is a match for text that cannot be saved in XML. Match == true.
        /// </summary>
        [CLSCompliant(false)]
        public static Regex InvalidUnicodeCharsExpression => s_RgxInvalidUnicodeCharsExpression.Value;

        /// <summary>
        /// Clipboard.
        /// </summary>
        public static XmlDocument Clipboard
        {
            get => _xmlClipboard;
            set
            {
                if (Interlocked.Exchange(ref _xmlClipboard, value) != value)
                    ClipboardChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(Clipboard)));
            }
        }

        /// <summary>
        /// Type of data that is currently stored in the clipboard.
        /// </summary>
        public static ClipboardContentType ClipboardContentType { get; set; }

        /// <summary>
        /// Default character sheet to use when printing.
        /// </summary>
        public static string DefaultCharacterSheet
        {
            get => settings.Print.DefaultPrintSheet;
            set => settings = settings with
            {
                Print = settings.Print with
                {
                    DefaultPrintSheet = value
                }
            };
        }

        /// <summary>
        /// Default setting to select when creating a new character
        /// </summary>
        public static string DefaultCharacterSetting
        {
            get => settings.Character.DefaultSettingsFile.ToString();
            set => settings = settings with
            {
                Character = settings.Character with
                {
                    DefaultSettingsFile = Guid.Parse(value)
                }
            };
        }

        /// <summary>
        /// Default setting to select when opening the Master Index for the first time
        /// </summary>
        public static string DefaultMasterIndexSetting
        {
            get => settings.UX.DefaultMasterIndexSettingsFile.ToString();
            set => settings = settings with
            {
                UX = settings.UX with
                {
                    DefaultMasterIndexSettingsFile = Guid.Parse(value)
                }
            };
        }

        /// <summary>
        /// Path to the user's PDF application.
        /// </summary>
        public static string PdfAppPath
        {
            get => settings.Pdf.ApplicationPath.FullName;
            set => settings = settings with
            {
                Pdf = settings.Pdf with
                {
                    ApplicationPath = new FileInfo(value)
                }
            };
        }

        /// <summary>
        /// Parameter style to use when opening a PDF with the PDF application specified in PdfAppPath
        /// </summary>
        public static string PdfParameters
        {
            get => settings.Pdf.ParametersStyle switch
            {
                PdfParametersStyle.WebBrowserStyle => "\"file://{absolutepath}#page={page}\"",
                PdfParametersStyle.AcrobatStyle => "/A \"page={page}\" \"{localpath}\"",
                PdfParametersStyle.AcrobatStyleNewInstance => "/A /N \"page={page}\" \"{localpath}\"",
                PdfParametersStyle.UnixStyle => "-p {page} \"{localpath}",
                PdfParametersStyle.SumatraStyle => "-reuse-instance -page {page} \"{localpath}\"",
                PdfParametersStyle.SumatraStyleReuseInstance => "-page {page} \"{localpath}\"",
                _ => throw new NotImplementedException(),
            };
            set => settings = settings with
            {
                Pdf = settings.Pdf with
                {
                    ParametersStyle = value switch
                    {
                        "\"file://{absolutepath}#page={page}\"" => PdfParametersStyle.WebBrowserStyle,
                        "/A \"page={page}\" \"{localpath}\"" => PdfParametersStyle.AcrobatStyle,
                        "/A /N \"page={page}\" \"{localpath}\"" => PdfParametersStyle.AcrobatStyleNewInstance,
                        "-p {page} \"{localpath}" => PdfParametersStyle.UnixStyle,
                        "-reuse-instance -page {page} \"{localpath}\"" => PdfParametersStyle.SumatraStyleReuseInstance,
                        "-page {page} \"{localpath}\"" => PdfParametersStyle.SumatraStyle,
                        _ => throw new NotImplementedException(value)
                    }
                }
            };
        }

        /// <summary>
        /// List of SourcebookInfo.
        /// </summary>
        public static IReadOnlyDictionary<string, SourcebookInfo> SourcebookInfos
        {
            get => settings.SourcebookInfo.Select(s => new SourcebookInfo()
            {
                Code = s.Key,
                Offset = s.PageOffset,
                Path = s.Path.FullName
            }).ToDictionary(si => si.Code);
        }

        /// <summary>
        /// List of SourcebookInfo.
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, SourcebookInfo>> GetSourcebookInfosAsync(CancellationToken token = default)
        {
            return SourcebookInfos;
        }

        public static async Task SetSourcebookInfosAsync(IReadOnlyDictionary<string, SourcebookInfo> dicNewValues, CancellationToken token = default)
        {
            var sb = new List<Sourcebook>(settings.SourcebookInfo);
            foreach ((string _, var sbi) in dicNewValues)
            {
                int index = sb.FindIndex(sb => sb.Key == sbi.Code);
                Sourcebook newsb = new Sourcebook(sbi.Code, new FileInfo(sbi.Path), sbi.Offset);
                if (index == -1)
                {
                    sb.Add(newsb);
                }
                else
                {
                    sb[index] = newsb;
                }
            }
            settings = settings with
            {
                SourcebookInfo = sb
            };
        }

        /// <summary>
        /// Dictionary of custom data directory infos keyed to their internal IDs.
        /// </summary>
        public static HashSet<CustomDataDirectoryInfo> CustomDataDirectoryInfos => s_SetCustomDataDirectoryInfos;

        /// <summary>
        /// Should the updater check for Release builds, or Nightly builds
        /// </summary>
        public static bool PreferNightlyBuilds
        {
            get => settings.Update.PreferNightly;
            set => settings = settings with
            {
                Update = settings.Update with
                {
                    PreferNightly = value
                }
            };
        }

        /// <summary>
        /// Path to the directory that Chummer should watch and from which to automatically populate its character roster.
        /// </summary>
        public static string CharacterRosterPath
        {
            get => settings.Character.RosterPath?.FullName;
            set => settings = settings with
            {
                Character = settings.Character with
                {
                    RosterPath = new DirectoryInfo(value)
                }
            };
        }

        /// <summary>
        /// Compat quality with old settings.
        /// </summary>
        public static int SavedImageQuality
        {
            get => settings.Saving.ImageCompressionLevel switch
            {
                ImageCompression.Undefined => throw new NotImplementedException(),
                ImageCompression.Png => int.MaxValue,
                ImageCompression.JpegAutomatic => -1,
                _ => (int)settings.Saving.ImageCompressionLevel,
            };
        }

        /// <summary>
        /// Compression quality to use when saving images.
        /// </summary>
        public static ImageCompression ImageCompressionLevel
        {
            get => settings.Saving.ImageCompressionLevel;
            set => settings = settings with
            {
                Saving = settings.Saving with
                {
                    ImageCompressionLevel = value
                }
            };
        }

        /// <summary>
        /// Converts an image to its Base64 string equivalent with compression settings specified by SavedImageQuality.
        /// </summary>
        /// <param name="objImageToSave">Image whose Base64 string should be created.</param>
        public static string ImageToBase64StringForStorage(Image objImageToSave)
        {
            return SavedImageQuality == int.MaxValue
                ? objImageToSave.ToBase64String()
                : objImageToSave.ToBase64StringAsJpeg(SavedImageQuality);
        }

        /// <summary>
        /// Converts an image to its Base64 string equivalent with compression settings specified by SavedImageQuality.
        /// </summary>
        /// <param name="objImageToSave">Image whose Base64 string should be created.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        public static Task<string> ImageToBase64StringForStorageAsync(Image objImageToSave, CancellationToken token = default)
        {
            return SavedImageQuality == int.MaxValue
                ? objImageToSave.ToBase64StringAsync(token: token)
                : objImageToSave.ToBase64StringAsJpegAsync(SavedImageQuality, token: token);
        }

        /// <summary>
        /// Last folder from which a mugshot was added
        /// </summary>
        public static string RecentImageFolder
        {
            get => settings.Saving.LastMugshotFolder?.FullName;
            set => settings = settings with
            {
                Saving = settings.Saving with
                {
                    LastMugshotFolder = new DirectoryInfo(value)
                }
            };
        }


        #region MRU Methods

        private static async Task LstFavoritedCharactersOnCollectionChanged(object sender,
            NotifyCollectionChangedEventArgs e, CancellationToken token = default)
        {
            settings = settings with
            {
                FavoriteCharacters = s_LstFavoriteCharacters.Select(s => new FileInfo(s)).ToList()
            };

            using (FileStream fs = settingsFile.Open(FileMode.Create, FileAccess.Write, FileShare.None))
            {
                gsm.SerializeGlobalSettings(settings, fs);
            }

            MruChanged?.Invoke(null, new TextEventArgs("stickymru"));
        }

        private static async Task LstMostRecentlyUsedCharactersOnCollectionChanged(object sender,
            NotifyCollectionChangedEventArgs e, CancellationToken token = default)
        {
            settings = settings with
            {
                MostRecentlyUsed = s_LstMostRecentlyUsedCharacters.Select(s => new FileInfo(s)).ToList()
            };

            using (FileStream fs = settingsFile.Open(FileMode.Create, FileAccess.Write, FileShare.None))
            {
                gsm.SerializeGlobalSettings(settings, fs);
            }

            MruChanged?.Invoke(null, new TextEventArgs("mru"));
        }

        public static MostRecentlyUsedCollection<string> FavoriteCharacters => s_LstFavoriteCharacters;

        public static MostRecentlyUsedCollection<string> MostRecentlyUsedCharacters => s_LstMostRecentlyUsedCharacters;

        public static LzmaHelper.ChummerCompressionPreset Chum5lzCompressionLevel
        {
            get => settings.Saving.SaveCompressionLevel switch
            {
                CompressionLevel.Fast => LzmaHelper.ChummerCompressionPreset.Fast,
                CompressionLevel.Balanced => LzmaHelper.ChummerCompressionPreset.Balanced,
                CompressionLevel.Thorough => LzmaHelper.ChummerCompressionPreset.Thorough,
                _ => throw new NotImplementedException(),
            };
            set => settings = settings with
            {
                Saving = settings.Saving with
                {
                    SaveCompressionLevel = Enum.Parse<CompressionLevel>(value.ToString())
                }
            };
        }

        public static bool CustomDateTimeFormats
        {
            get => settings.Display.CustomTimeFormat is not null
                || settings.Display.CustomDateFormat is not null;
            set
            {
                if (!value)
                {
                    settings = settings with
                    {
                        Display = settings.Display with
                        {
                            CustomDateFormat = null,
                            CustomTimeFormat = null
                        }
                    };
                }
            }
        }

        public static string CustomDateFormat
        {
            get => settings.Display.CustomDateFormat;
            set => settings = settings with
            {
                Display = settings.Display with
                {
                    CustomDateFormat = value
                }
            };
        }

        public static string CustomTimeFormat
        {
            get => settings.Display.CustomTimeFormat;
            set => settings = settings with
            {
                Display = settings.Display with
                {
                    CustomTimeFormat = value
                }
            };
        }

        /// <summary>
        /// Should the application assume that the Black Market Pipeline discount should automatically be used if the character has an appropriate contact?
        /// </summary>
        public static bool AssumeBlackMarket { get; set; }

        #endregion MRU Methods
    }
}
