using Chummer.Api.Models.Books;

namespace Chummer.Api
{
    public interface IDataLoader
    {
        IReadOnlyList<ChummerBook> LoadBooks();
    }
}
