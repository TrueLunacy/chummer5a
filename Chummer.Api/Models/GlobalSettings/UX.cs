namespace Chummer.Api.Models.GlobalSettings
{
    public record UX(bool SearchRestrictedToCurrentCategory, bool AskConfirmDelete,
            bool AskConfirmKarmaExpense, bool HideItemsOverAvailabilityLimitInCreate, bool AllowEasterEggs,
            bool HideMasterIndex, bool HideCharacterRoster, bool SingleDiceRoller,
            bool AllowScrollIncrement, bool AllowScrollTabSwitch, bool AllowSkillDiceRolling,
            bool SetTimeWithDate, Guid DefaultMasterIndexSettingsFile);
}
