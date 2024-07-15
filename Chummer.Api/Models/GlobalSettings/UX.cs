using RecordSourceGenerator.Generated;

namespace Chummer.Api.Models.GlobalSettings
{
    [XmlRecord]
    public sealed partial record UX(bool SearchRestrictedToCurrentCategory, bool AskConfirmDelete,
            bool AskConfirmKarmaExpense, bool HideItemsOverAvailabilityLimitInCreate, bool AllowEasterEggs,
            bool HideMasterIndex, bool HideCharacterRoster, bool SingleDiceRoller,
            bool AllowScrollIncrement, bool AllowScrollTabSwitch, bool AllowSkillDiceRolling,
            bool SetTimeWithDate, Guid DefaultMasterIndexSettingsFile);
}
