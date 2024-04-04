using System.Xml.Linq;

namespace Chummer.Api
{
    public interface IXmlFileProvider
    {
        public XDocument Books();
    }
}
