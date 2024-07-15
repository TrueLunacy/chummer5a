using Chummer.Api.Models.Books;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Chummer.Api
{

    public class ChummerDataLoader(IXmlFileProvider provider)
    {
        private IReadOnlyList<ChummerBook>? books;
        public IReadOnlyList<ChummerBook> LoadBooks()
        {
            if (books is null)
            {
                using var reader = provider.Books();
                ChummerRoot chummer = ChummerRoot.Read(reader);
                books = chummer.Books;
            }
            return this.books;
        }
    }
}
