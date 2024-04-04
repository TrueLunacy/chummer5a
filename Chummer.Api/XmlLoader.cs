using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Chummer.Api
{

    public class ChummerDataLoader
    {
        public ChummerDataLoader(IXmlFileProvider provider)
        {
            this.provider = provider;
        }

        private readonly IXmlFileProvider provider;

        private IReadOnlyList<ChummerBook>? books;
        public IReadOnlyList<ChummerBook> LoadBooks()
        {
            // todo: custom data stuff
            if (this.books is not null)
                return this.books;
            List<ChummerBook> books = new List<ChummerBook>();
            XDocument document = provider.Books();
            var bookCollection = document.Element("books")?.Elements().Where(e => e.Name == "Book")
                ?? Enumerable.Empty<XElement>();
            foreach (var bookElement in bookCollection)
            {
                if (bookElement.TryGetChildValue("id", out Guid id)
                    && bookElement.TryGetChildValue("name", out string? name)
                    && bookElement.TryGetChildValue("code", out string? code)
                )
                {
                    IReadOnlyList<ChummerBookMatch> matches = ParseMatches(bookElement.Element("matches"));
                    books.Add(new ChummerBook(id, name, code, matches));
                }
            }
            this.books = books;
            return books;

            static IReadOnlyList<ChummerBookMatch> ParseMatches(XElement? element)
            {
                if (element is null)
                    return [];
                List<ChummerBookMatch> matches = new();
                foreach (var match in element.Elements().Where(e => e.Name == "match"))
                {
                    if (match.TryGetChildValue("language", out string? langstr)
                        && match.TryGetChildValue("text", out string? text)
                        && match.TryGetChildValue("page", out int page))
                    {
                        try
                        {
                            matches.Add(new ChummerBookMatch(
                                CultureInfo.GetCultureInfo(langstr),
                                text, page));
                        }
                        catch (CultureNotFoundException)
                        {
                            continue;
                        }
                    }
                }
                return matches;
            }
        }
    }

    public record ChummerBook(Guid Id, string Name, string Code, IReadOnlyList<ChummerBookMatch> Matches);
    public record ChummerBookMatch(CultureInfo Language, string Text, int Page);
}
