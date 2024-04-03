using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Chummer.Api
{
    internal static class XmlUtilities
    {
        public static XmlWriterElementHelper OpenElement(this XmlWriter writer, string element)
        {
            return new XmlWriterElementHelper(writer, element);
        }


        public static T ChildValue<T>(this XElement element, string child, T defaultVal)
            where T : IParsable<T>
        {
            bool success = element.TryGetChildValue(child, out T? value);
            // value is be non-null if sucess is true, but nullability analysis doesn't pick it up for some reason
            // despite having the attribute
            return success ? value! : defaultVal;
        }

        public static T ChildValueEnum<T>(this XElement element, string child, T defaultVal)
            where T : struct, Enum
        {
            bool success = element.TryGetChildValue(child, out string? value);
            // needed because Enum.TryParse won't be run if success is false because shortcircuiting
            // and so the last line will complain that enumval is uninitialized even though it won't be
            // returned
            T enumval = default;
            success = success && Enum.TryParse(value, true, out enumval);
            return success ? enumval : defaultVal;
        }

        public static bool TryGetChildValue<T>(this XElement element, string child, [NotNullWhen(true)] out T? value)
            where T : IParsable<T>
        {
            bool success = element.TryGetChildValue(child, out string? str);
            if (success)
            {
                return T.TryParse(str, CultureInfo.InvariantCulture, out value);
            }
            else
            {
                value = default;
                return false;
            }
        }

        public static bool TryGetChildValue(this XElement element, string child, [NotNullWhen(true)] out string? value)
        {
            value = (string?)element.Single(child);
            return value != null;
        }

        [return: NotNullIfNotNull(nameof(useDefault))]
        public static FileInfo? ChildValue(this XElement element, string child, FileInfo? useDefault)
        {
            string? path = (string?)element.Single(child);
            if (path is null)
                return useDefault;
            return new FileInfo(path);
        }

        [return: NotNullIfNotNull(nameof(useDefault))]
        public static DirectoryInfo? ChildValue(this XElement element, string child, DirectoryInfo? useDefault)
        {
            string? path = (string?)element.Single(child);
            if (path is null)
                return useDefault;
            return new DirectoryInfo(path);
        }

        [return: NotNullIfNotNull(nameof(useDefault))]
        public static string? ChildValue(this XElement element, string child, string? useDefault)
        {
            string? str = (string?)element.Single(child);
            if (string.IsNullOrEmpty(str))
                return useDefault;
            return str;
        }

        /// <summary>
        /// This method returns a direct child XElement if and only if there is a single one with a matching name.
        /// If there are none, null is returned. If there are multiple, null is also returned.
        /// </summary>
        /// <param name="element">The XElement to search.</param>
        /// <param name="child">The name of the child element.</param>
        /// <returns>The child XElement, or null if there is not exactly one.</returns>
        public static XElement? Single(this XElement element, string child)
        {
            if (element.Elements().Count(e => e.Name == child) != 1)
                return null;
            return element.Element(child);
        }
    }

    
    internal readonly struct XmlWriterElementHelper : IDisposable
    {
        public readonly XmlWriter writer;

        internal XmlWriterElementHelper(XmlWriter writer, string element)
        {
            this.writer = writer;
            writer.WriteStartElement(element);
        }

        public void Dispose()
        {
            writer.WriteEndElement();
        }

        public XmlWriterElementHelper OpenElement(string element)
        {
            return new XmlWriterElementHelper(writer, element);
        }

        public void WriteElementValue(string element, bool o) { using var e = OpenElement(element); writer.WriteValue(o); }
        public void WriteElementValue(string element, DateTime o) { using var e = OpenElement(element); writer.WriteValue(o); }
        public void WriteElementValue(string element, DateTimeOffset o) { using var e = OpenElement(element); writer.WriteValue(o); }
        public void WriteElementValue(string element, decimal o) { using var e = OpenElement(element); writer.WriteValue(o); }
        public void WriteElementValue(string element, double o) { using var e = OpenElement(element); writer.WriteValue(o); }
        public void WriteElementValue(string element, int o) { using var e = OpenElement(element); writer.WriteValue(o); }
        public void WriteElementValue(string element, long o) { using var e = OpenElement(element); writer.WriteValue(o); }
        public void WriteElementValue(string element, float o) { using var e = OpenElement(element); writer.WriteValue(o); }
        public void WriteElementValue(string element, string o) { using var e = OpenElement(element); writer.WriteValue(o); }

        public void WriteElementValue<T>(string element, T e)
            where T : struct, Enum => WriteElementValue(element, e.ToString());
        public void WriteElementValue(string element, Guid guid)
            => WriteElementValue(element, guid.ToString());
    }
}
