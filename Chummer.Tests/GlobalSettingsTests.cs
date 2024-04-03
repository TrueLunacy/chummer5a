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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Threading;
using Xunit;
using Org.XmlUnit.Diff;
using Org.XmlUnit.Builder;
using Xunit.Sdk;
using Xunit.Abstractions;
using System.IO.Compression;
using Chummer.Api;
using System.Linq;
using Org.XmlUnit.Util;

namespace Chummer.Tests
{
    public class GlobalSettingsTests
    {
        private readonly ITestOutputHelper output;

        public GlobalSettingsTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void SerializeDeserializeDoesNotChangeContent()
        {
            Api.GlobalSettings settings = new(
                new Update(ShouldAutoUpdate: true, PreferNightly: true),
                new CustomData(AllowLiveUpdates: true, CustomDataDirectories: new List<DirectoryInfo>
                { 
                    new DirectoryInfo(Path.GetTempPath())
                }),
                new Pdf(ApplicationPath: new FileInfo(Path.GetTempFileName()), ParametersStyle: PdfParametersStyle.UnixStyle, InsertPdfNotes: false),
                new Print(PrintToFileFirst: true, PrintZeroRatingSkills: true, PrintExpenses: PrintExpenses.PrintAllExpenses,
                    PrintNotes: true, DefaultPrintSheet: "Shadowrun 7 (DO NOT LEAK)"),
                new Display(StartInFullscreenMode: true, ColorMode: Api.ColorMode.Dark, DpiScalingMethod: Api.DpiScalingMethod.SmartZoom,
                    CustomDateFormat: "mm dd yy", CustomTimeFormat: "tt:mm"),
                new UX(SearchRestrictedToCurrentCategory: false, AskConfirmDelete: false, AskConfirmKarmaExpense: false,
                    HideAvailabilityCreation: false, AllowEasterEggs: false, HideMasterIndex: true,
                    HideCharacterRoster: true, SingleDiceRoller: false, AllowScrollIncrement: true,
                    AllowScrollTabSwitch: true, AllowSkillDiceRolling: false, SetTimeWithDate: false,
                    DefaultMasterIndexSettingsFile: Guid.Parse("67e25032-9999-9999-97fa-69f7f608236c")),
                new Saving(SaveCompressionLevel: Api.CompressionLevel.Fast, ImageCompressionLevel: ImageCompression.JpegExtraExtraLow),
                new Logging(LogLevel: LogLevel.Crashes, LoggingResetCountdown: 69),
                new Api.Character(RosterPath: new DirectoryInfo(Path.GetTempPath()), CreateBackupOnCareer: false,
                    DefaultSettingsFile: Guid.Parse("223a11ff-9999-9999-89a9-6ef1c243b8b6"), LiveRefresh: true,
                    EnableLifeModules: true),
                Language: CultureInfo.GetCultureInfo("da-dk"),
                MostRecentlyUsed: new List<FileInfo>()
                {
                    new FileInfo(Path.GetTempFileName())
                },
                FavoriteCharacters: new List<FileInfo>()
                {
                    new FileInfo(Path.GetTempFileName())
                },
                SourcebookInfo: new List<Sourcebook>()
                {
                    new Sourcebook("SR7", new FileInfo(Path.GetTempFileName()), 112)
                }
            );
            // For best results, make sure the test settings above do not share any identical settings with the default settings
            // The usual approach when settings fail to load is to revert to default
            MemoryStream ms = new();
            GlobalSettingsManager gsm = new();
            gsm.SerializeGlobalSettings(settings, ms);
            ms.Seek(0, SeekOrigin.Begin);

            StreamReader sr = new StreamReader(ms);
            output.WriteLine(sr.ReadToEnd());
            ms.Seek(0, SeekOrigin.Begin);

            var newSettings = gsm.LoadGlobalSettings(ms);

            Assert.Equal(settings.Update, newSettings.Update);
            Assert.Equal(settings.CustomData.AllowLiveUpdates, newSettings.CustomData.AllowLiveUpdates);
            Assert.Equal(settings.CustomData.CustomDataDirectories.Count, newSettings.CustomData.CustomDataDirectories.Count);
            var di = Assert.Single(newSettings.CustomData.CustomDataDirectories);
            Assert.Equal(settings.CustomData.CustomDataDirectories.Single().FullName, di.FullName);
            // AppPath is probably done by reference comparision, so set it to null so it always succeeds
            // so we don't have to check every other thing individually
            Assert.Equal(settings.Pdf with { ApplicationPath = null },
                newSettings.Pdf with { ApplicationPath = null });
            Assert.Equal(settings.Pdf.ApplicationPath?.FullName, newSettings.Pdf.ApplicationPath?.FullName);
            Assert.Equal(settings.UX, newSettings.UX);
            Assert.Equal(settings.Saving, newSettings.Saving);
            Assert.Equal(settings.Logging, newSettings.Logging);
            // second verse same as the first
            Assert.Equal(settings.Character with { RosterPath = null }, 
                newSettings.Character with { RosterPath = null });
            Assert.Equal(settings.Character.RosterPath?.FullName, newSettings.Character.RosterPath?.FullName);
            Assert.Equal(settings.Language, newSettings.Language);
            var fi = Assert.Single(newSettings.MostRecentlyUsed);
            Assert.Equal(settings.MostRecentlyUsed.Single().FullName, fi.FullName);
            fi = Assert.Single(newSettings.FavoriteCharacters);
            Assert.Equal(settings.FavoriteCharacters.Single().FullName, fi.FullName);

            var sb = Assert.Single(settings.SourcebookInfo);
            var sb_e = settings.SourcebookInfo.Single();

            Assert.Equal(sb_e with { Path = null! }, sb with { Path = null! });
            Assert.Equal(sb_e.Path.FullName, sb.Path.FullName);
        }

    }
}
