using Microsoft.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

[assembly: InternalsVisibleTo("Chummer.Api")]

namespace Chummer.Xml
{
    public static class XmlUtilities
    {
        /// <summary>
        /// XmlReaderSettings that should be used when reading almost Xml readable.
        /// </summary>
        public static XmlReaderSettings SafeXmlReaderSettings { get; } = new XmlReaderSettings { XmlResolver = null, IgnoreComments = true, IgnoreWhitespace = true };

        /// <summary>
        /// XmlReaderSettings that should only be used if invalid characters are found.
        /// </summary>
        public static XmlReaderSettings UnSafeXmlReaderSettings { get; } = new XmlReaderSettings { XmlResolver = null, IgnoreComments = true, IgnoreWhitespace = true, CheckCharacters = false };

        private static readonly RecyclableMemoryStreamManager manager = new RecyclableMemoryStreamManager();
        public static RecyclableMemoryStream GetRecyclableStream(string tag, long length)
        {
            return new RecyclableMemoryStream(manager, tag, length);
        }
        public static RecyclableMemoryStream GetRecyclableStream()
        {
            return new RecyclableMemoryStream(manager);
        }

        private static readonly ConcurrentDictionary<string, XPathExpression> cache
            = new ConcurrentDictionary<string, XPathExpression>();

        // todo: a limit on size if this proves prohibitively large
        public static XPathExpression CacheExpression(string xpath)
        {
            return cache.GetOrAdd(xpath, XPathExpression.Compile);
        }

        //[Obsolete("Remove me")]
        internal static void RunOnMainThread(Action value)
        {
            value();
        }

        //[Obsolete("Remove me")]
        internal static Task RunOnMainThreadAsync(Action value, CancellationToken token = default)
        {
            value();
            return Task.CompletedTask;
        }

        //[Obsolete("Remove me")]
        internal static void RunWithoutThreadLock(List<Func<Task>> functions)
        {
            Task.WaitAll(functions.Select(f => f()).ToArray());
        }

        public const int MaxParallelBatchSize = 1;
    }
}
