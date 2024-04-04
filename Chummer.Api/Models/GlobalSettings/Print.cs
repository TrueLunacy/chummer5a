using Chummer.Api.Enums;

namespace Chummer.Api.Models.GlobalSettings
{
    public record Print(bool PrintToFileFirst, bool PrintZeroRatingSkills, PrintExpenses PrintExpenses,
        bool PrintNotes, string DefaultPrintSheet);
}
