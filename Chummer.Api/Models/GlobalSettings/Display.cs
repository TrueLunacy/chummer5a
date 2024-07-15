using Chummer.Api.Enums;
using RecordSourceGenerator.Generated;

namespace Chummer.Api.Models.GlobalSettings
{
    [XmlRecord]
    public sealed partial record Display(bool StartInFullscreenMode, ColorMode ColorMode, DpiScalingMethod DpiScalingMethod,
        string? CustomDateFormat, string? CustomTimeFormat);
}
