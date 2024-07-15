using RecordSourceGenerator.Generated;
using System.Collections.Immutable;

namespace Chummer.Api.Models.Books
{
    [XmlRecord(ElementName = "book")]
    public sealed partial record ChummerBook(
        [property: XmlAsElement(ElementName = "id")] Guid Id,
        [property: XmlAsElement(ElementName = "name")] string Name,
        [property: XmlAsElement(ElementName = "code")] string Code,
        [property: XmlAsPresence(ElementName = "permanent")] bool Permanent,
        [property: XmlAsPresence(ElementName = "hide")] bool Hide,
        [property: XmlAsElement(ElementName = "matches", ArrayElementName = "match")] ImmutableArray<ChummerBookMatch> Matches
    );
}
