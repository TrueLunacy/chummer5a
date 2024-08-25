using RecordSourceGenerator.Generated;
using System.Collections.Immutable;

namespace Chummer.Api.Models.Books
{
    [XmlRecord(ElementName = "chummer")]
    public sealed partial record ChummerRoot(
        [XmlAsElement(ElementName = "books", ArrayElementName = "book")] ImmutableArray<ChummerBook> Books
    );
}
