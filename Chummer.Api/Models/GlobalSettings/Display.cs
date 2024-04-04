using Chummer.Api.Enums;

namespace Chummer.Api.Models.GlobalSettings
{
    public record Display(bool StartInFullscreenMode, ColorMode ColorMode, DpiScalingMethod DpiScalingMethod,
        string? CustomDateFormat, string? CustomTimeFormat);
}
