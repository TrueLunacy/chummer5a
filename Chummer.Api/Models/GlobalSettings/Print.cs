using Chummer.Api.Enums;
using RecordSourceGenerator.Generated;

namespace Chummer.Api.Models.GlobalSettings
{
    [XmlRecord]
    public sealed partial record Print(bool PrintToFileFirst, bool PrintZeroRatingSkills, PrintExpenses PrintExpenses,
        bool PrintNotes, string DefaultPrintSheet);
}
