using Chummer.Api.Enums;

namespace Chummer.Api.Models.GlobalSettings
{
    public record Logging(LogLevel LogLevel, uint LoggingResetCountdown);
}
