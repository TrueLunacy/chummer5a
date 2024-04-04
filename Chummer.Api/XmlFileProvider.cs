using System.Xml.Linq;

namespace Chummer.Api
{
    public class XmlFileProvider : IXmlFileProvider
    {
        private readonly DirectoryInfo dataDirectory;

        public XmlFileProvider(DirectoryInfo dataDirectory)
        {
            this.dataDirectory = dataDirectory;
        }

        public XDocument Books()
        {
            using FileStream fs = dataDirectory.EnumerateFiles().Single(f => f.Name == "books.xml").OpenRead();
            return XDocument.Load(fs);
        }
    }
}
