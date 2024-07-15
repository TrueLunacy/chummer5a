// See https://aka.ms/new-console-template for more information
using Chummer.Api;
using Chummer.Api.Models.GlobalSettings;
using System.Collections.Immutable;
using System.Text;
using System.Xml;

Console.WriteLine("Hello, World!");

string xml = """
    <?xml version="1.0" encoding="utf-8"?>
    <GlobalSettings>
      <Update>
        <ShouldAutoUpdate>True</ShouldAutoUpdate>
        <PreferNightly>True</PreferNightly>
      </Update>
      <CustomData>
        <AllowLiveUpdates>True</AllowLiveUpdates>
        <CustomDataDirectories>
          <DirectoryInfo>C:\Program Files (x86)\Internet Explorer\</DirectoryInfo>
        </CustomDataDirectories>
      </CustomData>
      <Pdf>
        <ApplicationPath>C:\Program Files (x86)\Internet Explorer\iexplore.exe</ApplicationPath>
        <ParametersStyle>WebBrowserStyle</ParametersStyle>
        <InsertPdfNotes>True</InsertPdfNotes>
      </Pdf>
      <Print>
        <PrintToFileFirst>True</PrintToFileFirst>
        <PrintZeroRatingSkills>True</PrintZeroRatingSkills>
        <PrintExpenses>NoPrint</PrintExpenses>
        <PrintNotes>True</PrintNotes>
        <DefaultPrintSheet>Shadowrun 5 (Skills grouped by Rating greater 0)</DefaultPrintSheet>
      </Print>
      <Display>
        <StartInFullscreenMode>True</StartInFullscreenMode>
        <ColorMode>Automatic</ColorMode>
        <DpiScalingMethod>Zoom</DpiScalingMethod>
      </Display>
      <UX>
        <SearchRestrictedToCurrentCategory>True</SearchRestrictedToCurrentCategory>
        <AskConfirmDelete>True</AskConfirmDelete>
        <AskConfirmKarmaExpense>True</AskConfirmKarmaExpense>
        <HideItemsOverAvailabilityLimitInCreate>True</HideItemsOverAvailabilityLimitInCreate>
        <AllowEasterEggs>True</AllowEasterEggs>
        <HideMasterIndex>True</HideMasterIndex>
        <HideCharacterRoster>True</HideCharacterRoster>
        <SingleDiceRoller>True</SingleDiceRoller>
        <AllowScrollIncrement>True</AllowScrollIncrement>
        <AllowScrollTabSwitch>True</AllowScrollTabSwitch>
        <AllowSkillDiceRolling>True</AllowSkillDiceRolling>
        <SetTimeWithDate>True</SetTimeWithDate>
        <DefaultMasterIndexSettingsFile>91429237-05d5-4e84-8ab0-f00f0e769f64</DefaultMasterIndexSettingsFile>
      </UX>
      <Saving>
        <SaveCompressionLevel>Fast</SaveCompressionLevel>
        <ImageCompressionLevel>JpegExtraLow</ImageCompressionLevel>
      </Saving>
      <Logging>
        <LogLevel>NoLogging</LogLevel>
        <LoggingResetCountdown>2</LoggingResetCountdown>
      </Logging>
      <Character>
        <RosterPath>C:\Users\Lunacy\Contacts</RosterPath>
        <CreateBackupOnCareer>True</CreateBackupOnCareer>
        <DefaultSettingsFile>fe7bb0d9-3cd9-4a75-825e-135b95a4f3ef</DefaultSettingsFile>
        <LiveRefresh>True</LiveRefresh>
        <EnableLifeModules>True</EnableLifeModules>
      </Character>
      <Language>en-US</Language>
      <MostRecentlyUsed>
        <FileInfo>C:\Program Files (x86)\Internet Explorer\iexplore.exe</FileInfo>
      </MostRecentlyUsed>
      <FavoriteCharacters>
        <FileInfo>C:\Program Files (x86)\Internet Explorer\iexplore.exe</FileInfo>
      </FavoriteCharacters>
      <SourcebookInfo>
        <Sourcebook>
          <Key>SR</Key>
          <Path>C:\Program Files (x86)\Internet Explorer\iexplore.exe</Path>
          <PageOffset>12</PageOffset>
        </Sourcebook>
      </SourcebookInfo>
    </GlobalSettings>
    """;

Console.WriteLine("Start");
Console.WriteLine(xml);

var fileprovider = new XmlFileProvider(new DirectoryInfo("C:\\Users\\Lunacy\\Desktop\\chummer5a\\Chummer\\data"));
var loader = new ChummerDataLoader(fileprovider);

var books = loader.LoadBooks();

;
