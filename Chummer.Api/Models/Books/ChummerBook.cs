using RecordSourceGenerator.Generated;
using System.Collections.Immutable;

namespace Chummer.Api.Models.Books
{
    [XmlRecord(ElementName = "book")]
    public sealed partial record ChummerBook(
        [XmlAsElement(ElementName = "id")] Guid Id,
        [XmlAsElement(ElementName = "name")] string Name,
        [XmlAsElement(ElementName = "code")] string Code,
        [XmlAsPresence(ElementName = "permanent")] bool Permanent,
        [XmlAsPresence(ElementName = "hide")] bool Hide,
        [XmlAsElement(ElementName = "matches", ArrayElementName = "match")] ImmutableArray<ChummerBookMatch> Matches
    );
}
