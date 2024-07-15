using RecordSourceGenerator.Generated;

namespace Chummer.Api.Models.GlobalSettings
{
    [XmlRecord]
    public sealed partial record Update(bool ShouldAutoUpdate, bool PreferNightly);
}
