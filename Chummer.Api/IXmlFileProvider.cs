using System.Xml;
using System.Xml.Linq;

namespace Chummer.Api
{
    public interface IXmlFileProvider
    {
        XmlReader Books();
    }
}
