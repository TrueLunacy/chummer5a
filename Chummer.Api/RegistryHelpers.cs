using Microsoft.Win32;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Chummer.Api
{
    internal static class RegistryHelpers
    { 

        [return: NotNullIfNotNull(nameof(defaultValue))]
        public static T? LoadValueOrDefault<T>(this RegistryKey baseKey, string key, T? defaultValue)
            where T : IParsable<T>
        {
            Debug.Assert(OperatingSystem.IsWindows());
            object? value = baseKey.GetValue(key);
            if (value is not null && T.TryParse(value.ToString(), CultureInfo.InvariantCulture, out T? parsedValue))
                return parsedValue;
            return defaultValue;
        }

        public static T LoadEnumOrDefault<T>(this RegistryKey baseKey, string key, T defaultValue)
            where T : struct, Enum
        {
            Debug.Assert(OperatingSystem.IsWindows());
            object? value = baseKey.GetValue(key);
            if (value is not null && Enum.TryParse<T>(value.ToString(), true, out T parsedValue))
                return parsedValue;
            return defaultValue;
        }

        [return: NotNullIfNotNull(nameof(defaultValue))]
        public static FileInfo? LoadValueOrDefault(this RegistryKey baseKey, string key, FileInfo? defaultValue)
        {
            Debug.Assert(OperatingSystem.IsWindows());
            object? value = baseKey.GetValue(key);
            if (value is not null)
            {
                string? vstr = value.ToString();
                if (vstr is not null)
                    return new FileInfo(vstr);
            }
            return defaultValue;
        }

        [return: NotNullIfNotNull(nameof(defaultValue))]
        public static DirectoryInfo? LoadValueOrDefault(this RegistryKey baseKey, string key, DirectoryInfo? defaultValue)
        {
            Debug.Assert(OperatingSystem.IsWindows());
            object? value = baseKey.GetValue(key);
            if (value is not null)
            {
                string? vstr = value.ToString();
                if (vstr is not null)
                    return new DirectoryInfo(vstr);
            }
            return defaultValue;
        }

        public static bool TryLoadValue<T>(this RegistryKey baseKey, string key, [NotNullWhen(true)] out T? value)
            where T : IParsable<T>
        {
            Debug.Assert(OperatingSystem.IsWindows());
            object? objval = baseKey.GetValue(key);
            if (objval is not null)
                return T.TryParse(objval.ToString(), null, out value);
            value = default;
            return false;
        }
    }
}
