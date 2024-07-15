using Chummer.Api.Enums;
using RecordSourceGenerator.Generated;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;

namespace Chummer.Api.Models.GlobalSettings
{
    [XmlRecord]
    public sealed partial record Logging(LogLevel LogLevel, uint LoggingResetCountdown);
}
