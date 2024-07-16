/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */

using System.IO;
using Chummer.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chummer
{
    public static class TemporaryServiceLocator
    {
        public static IServiceProvider Services { get; }
        static TemporaryServiceLocator()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<ILegacySettingsManager, WindowsLegacySettingsManager>();
            services.AddSingleton<IGlobalSettingsManager, GlobalSettingsManager>();
            services.AddSingleton<IXmlFileProvider, XmlFileProvider>(s => new XmlFileProvider(new DirectoryInfo(Utils.GetDataFolderPath)));
            services.AddSingleton<IDataLoader, ChummerDataLoader>();
            services.AddLogging(b => b.AddConsole()
                .AddDebug()
                .AddEventSourceLogger());
            Services = services.BuildServiceProvider();
        }

        public static void Error(this ILogger logger, string msg)
        {
            logger.LogError(msg);
        }

        public static void Error(this ILogger logger, Exception exception)
        {
            logger.LogError(exception, null);
        }

        public static void Error(this ILogger logger, Exception exception, string msg)
        {
            logger.LogError(exception, msg);
        }

        public static void Info(this ILogger logger, string msg)
        {
            logger.LogInformation(msg);
        }

        public static void Info(this ILogger logger, Exception exception, string msg)
        {
            logger.LogInformation(exception, msg);
        }

        public static void Info(this ILogger logger, Exception exception)
        {
            logger.LogInformation(exception, null);
        }

        public static void Warn(this ILogger logger, string msg)
        {
            logger.LogWarning(msg);
        }

        public static void Warn(this ILogger logger, Exception exception)
        {
            logger.LogWarning(exception, null);
        }

        public static void Warn(this ILogger logger, Exception exception, string msg)
        {
            logger.LogWarning(exception, msg);
        }

        public static void Trace(this ILogger logger, string msg)
        {
            logger.LogTrace(msg);
        }

        public static void Trace(this ILogger logger, Exception exception)
        {
            logger.LogTrace(exception, null);
        }

        public static void Trace(this ILogger logger, Exception exception, string msg)
        {
            logger.LogTrace(exception, msg);
        }

        public static void Debug(this ILogger logger, string msg)
        {
            logger.LogDebug(msg);
        }

        public static void Debug(this ILogger logger, Exception exception)
        {
            logger.LogDebug(exception, null);
        }

        public static void Debug(this ILogger logger, Exception exception, string msg)
        {
            logger.LogDebug(exception, msg);
        }

        public static void Fatal(this ILogger logger, string msg)
        {
            logger.LogCritical(msg);
        }

        public static void Fatal(this ILogger logger, Exception exception)
        {
            logger.LogCritical(exception, null);
        }

        public static void Fatal(this ILogger logger, Exception exception, string msg)
        {
            logger.LogCritical(exception, msg);
        }

        [Obsolete("This function is a hack")]
        public static void Error(this ILogger logger, object[] array)
        {
            foreach (var obj in array)
            {
                logger.LogError(obj.ToString());
            }
        }

        [Obsolete("This function is a hack")]
        public static void Warn(this ILogger logger, object[] array)
        {
            foreach (var obj in array)
            {
                logger.LogWarning(obj.ToString());
            }
        }

        [Obsolete("This function is a hack")]
        public static void Info(this ILogger logger, object[] array)
        {
            foreach (var obj in array)
            {
                logger.LogInformation(obj.ToString());
            }
        }

        [Obsolete("This function is a hack")]
        public static void Debug(this ILogger logger, object[] array)
        {
            foreach (var obj in array)
            {
                logger.LogDebug(obj.ToString());
            }
        }
    }
}
