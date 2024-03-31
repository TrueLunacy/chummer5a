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

using RtfPipe;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Chummer.Xml
{
    public static class StringExtensions
    {
        public static string EmptyGuid { get; } = Guid.Empty.ToString("D", CultureInfo.InvariantCulture);

        public static bool IsEmptyGuid(this string str)
        {
            return str == EmptyGuid;
        }

        public static async Task<string> JoinAsync(string separator, IEnumerable<Task<string>> stringTasks,
                                                   CancellationToken token = default)
        {
            return string.Join(separator, await Task.WhenAll(stringTasks));
        }

        /// <summary>
        /// Method to quickly remove all instances of a char from a string (much faster than using Replace() with an empty string)
        /// </summary>
        /// <param name="input">String on which to operate</param>
        /// <param name="toRemove">Character to remove</param>
        /// <returns>New string with characters removed</returns>
        public static string FastEscape(this string input, char toRemove)
        {
            return input.Replace(toRemove.ToString(), "");
        }

        /// <summary>
        /// Method to quickly remove all instances of all chars in an array from a string (much faster than using a series of Replace() with an empty string)
        /// </summary>
        /// <param name="input">String on which to operate</param>
        /// <param name="toRemove">Array of characters to remove</param>
        /// <returns>New string with characters removed</returns>
        public static string FastEscape(this string input, params char[] toRemove)
        {
            foreach (char c in toRemove)
                input = input.Replace(c.ToString(), "");
            return input;
        }

        /// <summary>
        /// Method to quickly remove all instances of a substring from a string (should be faster than using Replace() with an empty string)
        /// </summary>
        /// <param name="input">String on which to operate</param>
        /// <param name="toDelete">Substring to remove</param>
        /// <param name="comparision">Comparison rules by which to find instances of the substring to remove. Useful for when case-insensitive removal is required.</param>
        /// <returns>New string with <paramref name="toDelete"/> removed</returns>
        public static string FastEscape(this string input, string toDelete,
                                        StringComparison comparision = StringComparison.Ordinal)
        {
            // It's actually faster to just run Replace(), albeit with our special comparison override, than to make our own fancy function
            return input.Replace(toDelete, string.Empty, comparision);
        }

        /// <summary>
        /// Method to quickly remove the first instance of a substring from a string.
        /// </summary>
        /// <param name="input">String on which to operate.</param>
        /// <param name="toDelete">Substring to remove.</param>
        /// <param name="startIndex">Index from which to begin searching.</param>
        /// <param name="comparision">Comparison rules by which to find the substring to remove. Useful for when case-insensitive removal is required.</param>
        /// <returns>New string with the first instance of <paramref name="toDelete"/> removed starting from <paramref name="startIndex"/>.</returns>
        public static string? FastEscapeOnceFromStart(this string? input, string? toDelete,
                                                     int startIndex = 0,
                                                     StringComparison comparision = StringComparison.Ordinal)
        {
            if (toDelete == null)
                return input;
            int intToDeleteLength = toDelete.Length;
            if (intToDeleteLength == 0)
                return input;
            if (input == null)
                return string.Empty;
            if (input.Length < intToDeleteLength)
                return input;

            int intIndexToBeginRemove = input.IndexOf(toDelete, startIndex, comparision);
            return intIndexToBeginRemove == -1 ? input : input.Remove(intIndexToBeginRemove, intToDeleteLength);
        }

        /// <summary>
        /// Method to quickly remove the last instance of a substring from a string.
        /// </summary>
        /// <param name="input">String on which to operate.</param>
        /// <param name="toDelete">Substring to remove.</param>
        /// <param name="index">Index from which to begin searching (proceeding towards the beginning of the string).</param>
        /// <param name="comparision">Comparison rules by which to find the substring to remove. Useful for when case-insensitive removal is required.</param>
        /// <returns>New string with the last instance of <paramref name="toDelete"/> removed starting from <paramref name="index"/>.</returns>
        public static string? FastEscapeOnceFromEnd(this string? input, string? toDelete,
                                                   int index = -1,
                                                   StringComparison comparision = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(input) || toDelete == null)
                return input;
            int intToDeleteLength = toDelete.Length;
            if (intToDeleteLength == 0)
                return input;
            if (index < 0)
                index += input.Length;
            if (index < intToDeleteLength - 1)
                return input;

            int intIndexToBeginRemove = input.LastIndexOf(toDelete, index, comparision);
            return intIndexToBeginRemove == -1 ? input : input.Remove(intIndexToBeginRemove, intToDeleteLength);
        }

        /// <summary>
        /// Syntactic sugar for string::IndexOfAny that uses params in its argument for the char array.
        /// </summary>
        /// <param name="haystack">String to search.</param>
        /// <param name="anyOf">Array of characters to match with IndexOfAny</param>
        /// <returns></returns>
        public static int IndexOfAny(this string haystack, params char[] anyOf)
        {
            return haystack.IndexOfAny(anyOf);
        }

        /// <summary>
        /// Find the index of the first instance of a set of strings inside a haystack string.
        /// </summary>
        /// <param name="haystack">String to search.</param>
        /// <param name="needles">Array of strings to match.</param>
        /// <param name="comparision">Comparison rules by which to find instances of the substring to remove. Useful for when case-insensitive removal is required.</param>
        /// <returns></returns>
        public static int IndexOfAny(this string haystack, IReadOnlyCollection<string> needles, StringComparison comparision)
        {
            if (string.IsNullOrEmpty(haystack))
                return -1;
            int intHaystackLength = haystack.Length;
            if (intHaystackLength == 0)
                return -1;
            if (needles == null)
                return -1;
            int intNumNeedles = needles.Count;
            if (intNumNeedles == 0)
                return -1;

            // While one might think this is the slowest, worst-scaling way of checking for multiple needles, it's actually faster
            // in C# than a more detailed approach where characters of the haystack are progressively checked against all needles.
            if (needles.All(x => x.Length > intHaystackLength))
                return -1;

            int intEarliestNeedleIndex = intHaystackLength;
            foreach (string strNeedle in needles)
            {
                int intNeedleIndex = haystack.IndexOf(strNeedle, 0, Math.Min(intHaystackLength, intEarliestNeedleIndex + strNeedle.Length), comparision);
                if (intNeedleIndex >= 0 && intNeedleIndex < intEarliestNeedleIndex)
                    intEarliestNeedleIndex = intNeedleIndex;
            }
            return intEarliestNeedleIndex != intHaystackLength ? intEarliestNeedleIndex : -1;
        }

        /// <summary>
        /// Find if of a haystack string contains any of a set of strings.
        /// </summary>
        /// <param name="haystack">String to search.</param>
        /// <param name="needles">Array of strings to match.</param>
        /// <param name="comparision">Comparison rules by which to find instances of the substring to remove. Useful for when case-insensitive removal is required.</param>
        /// <returns></returns>
        public static bool ContainsAny(this string haystack, IEnumerable<string> needles, StringComparison comparision = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(haystack))
                return false;
            if (needles == null)
                return false;

            return needles.Any(x => haystack.Contains(x, comparision));
        }

        /// <summary>
        /// Find if of a haystack string contains any of a set of strings.
        /// </summary>
        /// <param name="haystack">String to search.</param>
        /// <param name="needles">Array of strings to match.</param>
        /// <returns></returns>
        public static bool ContainsAny(this string haystack, params string[] needles)
        {
            return haystack.ContainsAny(needles, StringComparison.Ordinal);
        }

        /// <summary>
        /// Find if of a haystack string contains any of a set of strings (parallelized version where each needle is checked in parallel).
        /// </summary>
        /// <param name="haystack">String to search.</param>
        /// <param name="needles">Array of strings to match.</param>
        /// <param name="comparision">Comparison rules by which to find instances of the substring to remove. Useful for when case-insensitive removal is required.</param>
        /// <returns></returns>
        public static bool ContainsAnyParallel(this string haystack, IReadOnlyCollection<string> needles, StringComparison comparision)
        {
            // It feels unlikely parallel computation is truly needed
            return haystack.ContainsAny(needles, comparision);
        }


        /// <summary>
        /// Version of string::Split() that avoids allocations where possible, thus making it lighter on memory (and also on CPU because allocations take time) than all versions of string::Split()
        /// </summary>
        /// <param name="input">Input textblock.</param>
        /// <param name="separator">Character to use for splitting.</param>
        /// <param name="options">Optional argument that can be used to skip over empty entries.</param>
        /// <returns>Enumerable containing substrings of <paramref name="input"/> split based on <paramref name="separator"/></returns>
        public static IEnumerable<string> SplitNoAlloc(this string input, char separator,
                                                       StringSplitOptions options = StringSplitOptions.None)
        {
            return input.Split(separator, options);
        }

        /// <summary>
        /// Version of string::Split() that avoids allocations where possible, thus making it lighter on memory (and also on CPU because allocations take time) than all versions of string::Split()
        /// </summary>
        /// <param name="input">Input textblock.</param>
        /// <param name="separator">String to use for splitting.</param>
        /// <param name="options">Optional argument that can be used to skip over empty entries.</param>
        /// <returns>Enumerable containing substrings of <paramref name="input"/> split based on <paramref name="separator"/></returns>
        public static IEnumerable<string> SplitNoAlloc(this string input, string separator,
                                                       StringSplitOptions options = StringSplitOptions.None)
        {
            return input.Split(separator, options);
        }

        /// <summary>
        /// Version of string::Split() that avoids allocations where possible, thus making it lighter on memory (and also on CPU because allocations take time) than all versions of string::Split()
        /// </summary>
        /// <param name="input">Input textblock.</param>
        /// <param name="separators">Characters to use for splitting.</param>
        /// <returns>Enumerable containing substrings of <paramref name="input"/> split based on <paramref name="separators"/></returns>
        public static IEnumerable<string> SplitNoAlloc(this string input, params char[] separators)
        {
            return input.Split(separators);
        }

        /// <summary>
        /// Normalizes whitespace for a given textblock, removing extra spaces and trimming the string.
        /// </summary>
        /// <param name="input">Input textblock</param>
        /// <param name="funcIsWhiteSpace">Custom function with which to check if a character should count as whitespace. If null, defaults to char::IsWhiteSpace && !char::IsControl.</param>
        /// <returns>New string with any chars that return true from <paramref name="funcIsWhiteSpace"/> replaced with the first whitespace in a sequence and any excess whitespace removed.</returns>
        public static string NormalizeWhiteSpace(this string input)
        {
            if (input == null)
                return string.Empty;
            int intLength = input.Length;
            if (intLength == 0)
                return input;
            static bool funcIsWhiteSpace(char x) => char.IsWhiteSpace(x) && !char.IsControl(x);
            string strReturn;
            char[] achrNewChars = ArrayPool<char>.Shared.Rent(intLength);
            try
            {
                // What we're going here is copying the string-as-CharArray char-by-char into a new CharArray, but processing whitespace characters differently...
                int intCurrent = 0;
                int intLoopWhitespaceCount = 0;
                bool blnTrimMode = true;
                char chrLastAddedCharacter = ' ';
                for (int i = 0; i < intLength; ++i)
                {
                    char chrLoop = input[i];
                    // If we encounter a block of identical whitespace chars, we replace the first instance with chrWhiteSpace, then skip over the rest until we encounter a char that isn't whitespace
                    if (funcIsWhiteSpace(chrLoop))
                    {
                        ++intLoopWhitespaceCount;
                        if (chrLastAddedCharacter != chrLoop && !blnTrimMode)
                        {
                            achrNewChars[intCurrent++] = chrLoop;
                            chrLastAddedCharacter = chrLoop;
                        }
                    }
                    else
                    {
                        intLoopWhitespaceCount = 0;
                        blnTrimMode = false;
                        achrNewChars[intCurrent++] = chrLoop;
                        chrLastAddedCharacter = chrLoop;
                    }
                }

                // If all we had was whitespace, return a string with just a single space character
                if (intLoopWhitespaceCount >= intCurrent)
                {
                    return " ";
                }

                // ... then we create a new string from the new CharArray, but only up to the number of characters that actually ended up getting copied.
                // If the last char is whitespace, we don't copy that, either.
                strReturn = new string(achrNewChars, 0, intCurrent - intLoopWhitespaceCount);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(achrNewChars);
            }

            return strReturn;

        }

        /// <summary>
        /// Returns whether a string contains only legal characters.
        /// </summary>
        /// <param name="input">String to check.</param>
        /// <param name="isWhitelist">Whether the list of chars is a whitelist and the string can only contain characters in the list (true) or a blacklist and the string cannot contain any characts in the list (false).</param>
        /// <param name="chars">List of chars against which to check the string.</param>
        /// <returns>True if the string contains only legal characters, false if the string contains at least one illegal character.</returns>
        public static bool IsLegalCharsOnly(this string input, bool isWhitelist, IReadOnlyList<char> chars)
        {
            if (input == null)
                return false;
            int intLength = input.Length;
            if (intLength == 0)
                return true;
            int intLegalCharsLength = chars.Count;
            if (intLegalCharsLength == 0)
                return true;
            for (int i = 0; i < intLength; ++i)
            {
                char chrLoop = input[i];
                bool blnCharIsInList = false;
                for (int j = 0; j < intLegalCharsLength; ++j)
                {
                    if (chrLoop == chars[j])
                    {
                        blnCharIsInList = true;
                        break;
                    }
                }

                if (blnCharIsInList != isWhitelist)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Trims a substring out of the beginning of a string. If the substring appears multiple times at the beginning, all instances of it will be trimmed.
        /// </summary>
        /// <param name="input">String on which to operate</param>
        /// <param name="substring">Substring to trim</param>
        /// <param name="comparision">Comparison rules by which to find the substring to remove. Useful for when case-insensitive removal is required.</param>
        /// <returns>Trimmed String</returns>
        public static string TrimStart(this string input, string substring,
                                       StringComparison comparision = StringComparison.Ordinal)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(substring))
                return input;
            int intTrimLength = substring.Length;
            if (intTrimLength == 1)
                return input.TrimStart(substring[0]);

            int i = input.IndexOf(substring, comparision);
            if (i == -1)
                return input;

            int intAmountToTrim = 0;
            do
            {
                intAmountToTrim += intTrimLength;
                i = input.IndexOf(substring, intAmountToTrim, comparision);
            } while (i != -1);

            return input.Substring(intAmountToTrim);
        }


        /// <summary>
        /// Escapes a substring once out of a string if the string begins with it.
        /// </summary>
        /// <param name="input">String on which to operate</param>
        /// <param name="substring">Substring to escape</param>
        /// <param name="blnOmitCheck">If we already know that the string begins with the substring</param>
        /// <returns>String with <paramref name="substring"/> escaped out once from the beginning of it.</returns>
        public static string TrimStartOnce(this string input, string substring, bool blnOmitCheck = false)
        {
            if (!string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(substring)
                                                // Need to make sure string actually starts with the substring, otherwise we don't want to be cutting out the beginning of the string
                                                && (blnOmitCheck
                                                    || input.StartsWith(substring, StringComparison.Ordinal)))
            {
                int intTrimLength = substring.Length;
                return input.Substring(intTrimLength, input.Length - intTrimLength);
            }

            return input;
        }

        /// <summary>
        /// Escapes a char once out of a string if the string begins with it.
        /// </summary>
        /// <param name="strInput">String on which to operate</param>
        /// <param name="chrToTrim">Char to escape</param>
        /// <returns>String with <paramref name="chrToTrim"/> escaped out once from the beginning of it.</returns>
        public static string TrimStartOnce(this string strInput, char chrToTrim)
        {
            if (!string.IsNullOrEmpty(strInput) && strInput[0] == chrToTrim)
            {
                return strInput[1..];
            }

            return strInput;
        }

        /// <summary>
        /// Trims a substring out of a string if the string ends with it.
        /// </summary>
        /// <param name="strInput">String on which to operate</param>
        /// <param name="strToTrim">Substring to trim</param>
        /// <param name="blnOmitCheck">If we already know that the string ends with the substring</param>
        /// <returns>Trimmed String</returns>
        public static string TrimEndOnce(this string strInput, string strToTrim, bool blnOmitCheck = false)
        {
            if (!string.IsNullOrEmpty(strInput) && !string.IsNullOrEmpty(strToTrim)
                                                // Need to make sure string actually ends with the substring, otherwise we don't want to be cutting out the end of the string
                                                && (blnOmitCheck
                                                    || strInput.EndsWith(strToTrim, StringComparison.Ordinal)))
            {
                return strInput.Substring(0, strInput.Length - strToTrim.Length);
            }

            return strInput;
        }

        /// <summary>
        /// If a string ends with any substrings, the one with which it begins is trimmed out of the string once.
        /// </summary>
        /// <param name="strInput">String on which to operate</param>
        /// <param name="astrToTrim">Substrings to trim</param>
        /// <returns>Trimmed String</returns>
        public static string TrimEndOnce(this string strInput, params string[] astrToTrim)
        {
            if (!string.IsNullOrEmpty(strInput) && astrToTrim != null)
            {
                // Without this we could trim a smaller string just because it was found first, this makes sure we find the largest one
                int intHowMuchToTrim = 0;

                int intLength = astrToTrim.Length;
                for (int i = 0; i < intLength; ++i)
                {
                    string strStringToTrim = astrToTrim[i];
                    // Need to make sure string actually ends with the substring, otherwise we don't want to be cutting out the end of the string
                    if (strStringToTrim.Length > intHowMuchToTrim
                        && strInput.EndsWith(strStringToTrim, StringComparison.Ordinal))
                    {
                        intHowMuchToTrim = strStringToTrim.Length;
                    }
                }

                if (intHowMuchToTrim > 0)
                    return strInput.Substring(0, strInput.Length - intHowMuchToTrim);
            }

            return strInput;
        }

        /// <summary>
        /// Trims a char out of a string if the string ends with it.
        /// </summary>
        /// <param name="strInput">String on which to operate</param>
        /// <param name="chrToTrim">Char to trim</param>
        /// <returns>Trimmed String</returns>
        public static string TrimEndOnce(this string strInput, char chrToTrim)
        {
            if (!string.IsNullOrEmpty(strInput))
            {
                int intLength = strInput.Length;
                if (strInput[intLength - 1] == chrToTrim)
                    return strInput.Substring(0, intLength - 1);
            }

            return strInput;
        }

        /// <summary>
        /// If a string ends with any chars, the one with which it begins is trimmed out of the string once.
        /// </summary>
        /// <param name="strInput">String on which to operate</param>
        /// <param name="achrToTrim">Chars to trim</param>
        /// <returns>Trimmed String</returns>
        public static string TrimEndOnce(this string strInput, params char[] achrToTrim)
        {
            if (!string.IsNullOrEmpty(strInput) && strInput.EndsWith(achrToTrim))
                return strInput.Substring(0, strInput.Length - 1);
            return strInput;
        }

        /// <summary>
        /// Determines whether the first char of this string instance matches any of the specified chars.
        /// </summary>
        /// <param name="strInput">String to check.</param>
        /// <param name="achrToCheck">Chars to check.</param>
        /// <returns>True if string has a non-zero length and begins with any of the specified chars, false otherwise.</returns>        public static bool StartsWith(this string strInput, params char[] achrToCheck)
        {
            if (string.IsNullOrEmpty(strInput) || achrToCheck == null)
                return false;
            char chrCharToCheck = strInput[0];
            int intParamsLength = achrToCheck.Length;
            for (int i = 0; i < intParamsLength; ++i)
            {
                if (chrCharToCheck == achrToCheck[i])
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the last char of this string instance matches any of the specified chars.
        /// </summary>
        /// <param name="strInput">String to check.</param>
        /// <param name="achrToCheck">Chars to check.</param>
        /// <returns>True if string has a non-zero length and ends with any of the specified chars, false otherwise.</returns>
        public static bool EndsWith(this string strInput, params char[] achrToCheck)
        {
            if (strInput == null || achrToCheck == null)
                return false;
            int intLength = strInput.Length;
            if (intLength == 0)
                return false;
            char chrCharToCheck = strInput[intLength - 1];
            int intParamsLength = achrToCheck.Length;
            for (int i = 0; i < intParamsLength; ++i)
            {
                if (chrCharToCheck == achrToCheck[i])
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the end of this string instance matches any of the specified strings.
        /// </summary>
        /// <param name="strInput">String to check.</param>
        /// <param name="astrToCheck">Strings to check.</param>
        /// <returns>True if string has a non-zero length and ends with any of the specified chars, false otherwise.</returns>        public static bool EndsWith(this string strInput, params string[] astrToCheck)
        {
            if (!string.IsNullOrEmpty(strInput) && astrToCheck != null)
            {
                int intLength = astrToCheck.Length;
                for (int i = 0; i < intLength; ++i)
                {
                    if (strInput.EndsWith(astrToCheck[i], StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Like string::Replace(), but meant for if the new value would be expensive to calculate. Actually slower than string::Replace() if the new value is something simple.
        /// If the string does not contain any instances of the pattern to replace, then the expensive method to generate a replacement is not run.
        /// </summary>
        /// <param name="strInput">Base string in which the replacing takes place.</param>
        /// <param name="strOldValue">Pattern for which to check and which to replace.</param>
        /// <param name="funcNewValueFactory">Function to generate the string that replaces the pattern in the base string.</param>
        /// <param name="eStringComparison">The StringComparison to use for finding and replacing items.</param>
        /// <returns>The result of a string::Replace() method if a replacement is made, the original string otherwise.</returns>        public static string CheapReplace(this string strInput, string strOldValue, Func<string> funcNewValueFactory,
                                          StringComparison eStringComparison = StringComparison.Ordinal)
        {
            if (!string.IsNullOrEmpty(strInput) && funcNewValueFactory != null)
            {
                if (eStringComparison == StringComparison.Ordinal)
                {
                    if (strInput.Contains(strOldValue))
                        return strInput.Replace(strOldValue, funcNewValueFactory.Invoke());
                }
                else if (strInput.IndexOf(strOldValue, eStringComparison) != -1)
                    return strInput.Replace(strOldValue, funcNewValueFactory.Invoke(), eStringComparison);
            }

            return strInput;
        }

        /// <summary>
        /// Like string::Replace(), but meant for if the new value would be expensive to calculate. Actually slower than string::Replace() if the new value is something simple.
        /// This is the async version that can be run in case a value is really expensive to get.
        /// If the string does not contain any instances of the pattern to replace, then the expensive method to generate a replacement is not run.
        /// </summary>
        /// <param name="strInput">Base string in which the replacing takes place.</param>
        /// <param name="strOldValue">Pattern for which to check and which to replace.</param>
        /// <param name="funcNewValueFactory">Function to generate the string that replaces the pattern in the base string.</param>
        /// <param name="eStringComparison">The StringComparison to use for finding and replacing items.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns>The result of a string::Replace() method if a replacement is made, the original string otherwise.</returns>        public static async Task<string> CheapReplaceAsync(this string strInput, string strOldValue,
                                                           Func<string> funcNewValueFactory,
                                                           StringComparison eStringComparison
                                                               = StringComparison.Ordinal,
                                                           CancellationToken token = default)
        {
            var newvalue = funcNewValueFactory();
            return strInput.Replace(strOldValue, newvalue, eStringComparison);
        }

        /// <summary>
        /// Like string::Replace(), but meant for if the new value would be expensive to calculate. Actually slower than string::Replace() if the new value is something simple.
        /// This is the async version that can be run in case a value is really expensive to get.
        /// If the string does not contain any instances of the pattern to replace, then the expensive method to generate a replacement is not run.
        /// </summary>
        /// <param name="strInputTask">Task returning the base string in which the replacing takes place.</param>
        /// <param name="strOldValue">Pattern for which to check and which to replace.</param>
        /// <param name="funcNewValueFactory">Function to generate the string that replaces the pattern in the base string.</param>
        /// <param name="eStringComparison">The StringComparison to use for finding and replacing items.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns>The result of a string::Replace() method if a replacement is made, the original string otherwise.</returns>        public static async Task<string> CheapReplaceAsync(this Task<string> strInputTask, string strOldValue,
                                                           Func<string> funcNewValueFactory,
                                                           StringComparison eStringComparison
                                                               = StringComparison.Ordinal,
                                                           CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await CheapReplaceAsync(await strInputTask.ConfigureAwait(false), strOldValue, funcNewValueFactory,
                                           eStringComparison, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Like string::Replace(), but meant for if the new value would be expensive to calculate. Actually slower than string::Replace() if the new value is something simple.
        /// This is the async version that can be run in case a value is really expensive to get.
        /// If the string does not contain any instances of the pattern to replace, then the expensive method to generate a replacement is not run.
        /// </summary>
        /// <param name="strInput">Base string in which the replacing takes place.</param>
        /// <param name="strOldValue">Pattern for which to check and which to replace.</param>
        /// <param name="funcNewValueFactory">Function to generate the string that replaces the pattern in the base string.</param>
        /// <param name="eStringComparison">The StringComparison to use for finding and replacing items.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns>The result of a string::Replace() method if a replacement is made, the original string otherwise.</returns>        public static async Task<string> CheapReplaceAsync(this string strInput, string strOldValue,
                                                           Func<Task<string>> funcNewValueFactory,
                                                           StringComparison eStringComparison
                                                               = StringComparison.Ordinal,
                                                           CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(strInput) && funcNewValueFactory != null)
            {
                if (eStringComparison == StringComparison.Ordinal)
                {
                    if (strInput.Contains(strOldValue))
                    {
                        token.ThrowIfCancellationRequested();
                        string strNewValue = await funcNewValueFactory.Invoke().ConfigureAwait(false);
                        token.ThrowIfCancellationRequested();
                        return strInput.Replace(strOldValue, strNewValue);
                    }
                }
                else if (strInput.IndexOf(strOldValue, eStringComparison) != -1)
                {
                    token.ThrowIfCancellationRequested();
                    string strNewValue = await funcNewValueFactory.Invoke().ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    return strInput.Replace(strOldValue, strNewValue, eStringComparison);
                }
            }

            return strInput;
        }

        /// <summary>
        /// Like string::Replace(), but meant for if the new value would be expensive to calculate. Actually slower than string::Replace() if the new value is something simple.
        /// This is the async version that can be run in case a value is really expensive to get.
        /// If the string does not contain any instances of the pattern to replace, then the expensive method to generate a replacement is not run.
        /// </summary>
        /// <param name="strInputTask">Task returning the base string in which the replacing takes place.</param>
        /// <param name="strOldValue">Pattern for which to check and which to replace.</param>
        /// <param name="funcNewValueFactory">Function to generate the string that replaces the pattern in the base string.</param>
        /// <param name="eStringComparison">The StringComparison to use for finding and replacing items.</param>
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns>The result of a string::Replace() method if a replacement is made, the original string otherwise.</returns>        public static async Task<string> CheapReplaceAsync(this Task<string> strInputTask, string strOldValue,
                                                           Func<Task<string>> funcNewValueFactory,
                                                           StringComparison eStringComparison
                                                               = StringComparison.Ordinal,
                                                           CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await CheapReplaceAsync(await strInputTask.ConfigureAwait(false), strOldValue, funcNewValueFactory,
                                           eStringComparison, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Tests whether a given string is a Guid. Returns false if not.
        /// </summary>
        /// <param name="strGuid">String to test.</param>
        /// <returns>True if string is a Guid, false if not.</returns>        public static bool IsGuid(this string strGuid)
        {
            return Guid.TryParse(strGuid, out Guid _);
        }

        private static readonly Dictionary<string, string> s_DicLigaturesMap = new Dictionary<string, string>
        {
            {"ﬀ", "ff"},
            {"ﬃ", "ffi"},
            {"ﬄ", "ffl"},
            {"ﬁ", "fi"},
            {"ﬂ", "fl"},
            // Some PDF fonts have this control character defined as the "fi" ligature for some reason.
            // It's dumb and will cause XML errors, so it definitely has to be replaced/cleaned.
            {'\u001f'.ToString(), "fi"}
        };

        /// <summary>
        /// Replace some of the bad ligatures that are present in Shadowrun sourcebooks with proper characters
        /// </summary>
        /// <param name="strInput">String to clean.</param>
        /// <returns>Cleaned string with bad ligatures replaced with full latin characters</returns>
        public static string CleanStylisticLigatures(this string strInput)
        {
            if (string.IsNullOrEmpty(strInput))
                return strInput;
            string strReturn = strInput;
            foreach (KeyValuePair<string, string> kvpLigature in s_DicLigaturesMap)
                strReturn = strReturn.Replace(kvpLigature.Key, kvpLigature.Value);
            return strReturn;
        }

        /// <summary>
        /// Word wraps the given text to fit within the specified width.
        /// </summary>
        /// <param name="strText">Text to be word wrapped</param>
        /// <param name="intWidth">Width, in characters, to which the text should be word wrapped</param>
        /// <returns>The modified text</returns>
        public static string WordWrap(this string strText, int intWidth = 128)
        {
            // Lucidity checks
            if (string.IsNullOrEmpty(strText))
                return strText;
            if (intWidth >= strText.Length)
                return strText;

            var sbdReturn = new StringBuilder();
            {
                int intNewCapacity = strText.Length;
                if (sbdReturn.Capacity < intNewCapacity)
                    sbdReturn.Capacity = intNewCapacity;
                string strNewLine = Environment.NewLine;
                // Parse each line of text
                int intNextPosition;
                for (int intCurrentPosition = 0;
                     intCurrentPosition < strText.Length;
                     intCurrentPosition = intNextPosition)
                {
                    // Find end of line
                    int intEndOfLinePosition
                        = strText.IndexOf(strNewLine, intCurrentPosition, StringComparison.Ordinal);
                    if (intEndOfLinePosition == -1)
                        intNextPosition = intEndOfLinePosition = strText.Length;
                    else
                        intNextPosition = intEndOfLinePosition + strNewLine.Length;

                    // Copy this line of text, breaking into smaller lines as needed
                    if (intEndOfLinePosition > intCurrentPosition)
                    {
                        do
                        {
                            int intLengthToRead = intEndOfLinePosition - intCurrentPosition;
                            if (intLengthToRead > intWidth)
                                intLengthToRead = strText.BreakLine(intCurrentPosition, intWidth);
                            sbdReturn.Append(strText, intCurrentPosition, intLengthToRead).AppendLine();

                            // Trim whitespace following break
                            intCurrentPosition += intLengthToRead;
                            while (intCurrentPosition < intEndOfLinePosition
                                   && char.IsWhiteSpace(strText[intCurrentPosition])
                                   && !char.IsControl(strText[intCurrentPosition]))
                                ++intCurrentPosition;
                        } while (intEndOfLinePosition > intCurrentPosition);
                    }
                    else
                        sbdReturn.AppendLine(); // Empty line
                }

                return sbdReturn.ToString();
            }
        }

        /// <summary>
        /// Checks if every letter in a string is uppercase or not.
        /// </summary>
        public static bool IsAllLettersUpperCase(this string strText)
        {
            if (strText == null)
                throw new ArgumentNullException(nameof(strText));
            return string.IsNullOrEmpty(strText) || strText.All(x => !char.IsLetter(x) || char.IsUpper(x));
        }

        /// <summary>
        /// Locates position to break the given line so as to avoid
        /// breaking words.
        /// </summary>
        /// <param name="strText">String that contains line of text</param>
        /// <param name="intPosition">Index where line of text starts</param>
        /// <param name="intMax">Maximum line length</param>
        /// <returns>The modified line length</returns>
        private static int BreakLine(this string strText, int intPosition, int intMax)
        {
            if (strText == null)
                return intMax;
            if (intMax + intPosition >= strText.Length)
                return intMax;
            // Find last whitespace in line
            for (int i = intMax; i >= 0; --i)
            {
                char chrLoop = strText[intPosition + i];
                if (!char.IsControl(chrLoop)
                    && chrLoop != '\u00A0' // Non-breaking spaces should not break lines
                    && chrLoop != '\u202F' // Non-breaking spaces should not break lines
                    && chrLoop != '\uFEFF' // Non-breaking spaces should not break lines
                    && (char.IsWhiteSpace(chrLoop)
                        || chrLoop == '\u00AD')) // Soft hyphens allow breakage
                {
                    // Return length of text before whitespace
                    return i + 1;
                }
            }

            // If no whitespace found, break at maximum length
            return intMax;
        }

        /// <summary>
        /// Normalizes line endings to always be that of Environment.NewLine.
        /// </summary>
        /// <param name="strInput">String to normalize.</param>
        /// <param name="blnEscaped">If the line endings in the string are defined in an escaped fashion (e.g. as "\\n"), set to true.</param>
        /// <returns></returns>
        public static string NormalizeLineEndings(this string strInput, bool blnEscaped = false)
        {
            if (string.IsNullOrEmpty(strInput))
                return strInput;
            return blnEscaped
                ? s_RgxEscapedLineEndingsExpression.Value.Replace(strInput, Environment.NewLine)
                : s_RgxLineEndingsExpression.Value.Replace(strInput, Environment.NewLine);
        }

        /// <summary>
        /// Clean a string for usage inside an XPath filter, also surrounding it with quotation marks in an appropriate way.
        /// </summary>
        /// <param name="strSearch">String to clean.</param>
        public static string CleanXPath(this string strSearch)
        {
            if (string.IsNullOrEmpty(strSearch))
                return "\"\"";
            int intQuotePos = strSearch.IndexOf('"');
            if (intQuotePos == -1)
            {
                return '\"' + strSearch + '\"';
            }

            var sbdReturn = new StringBuilder();

            {
                int intNewCapacity = strSearch.Length + 10;
                if (sbdReturn.Capacity < intNewCapacity)
                    sbdReturn.Capacity = intNewCapacity;
                sbdReturn.Append("concat(\"");
                int intSubStringStart = 0;
                for (; intQuotePos != -1; intQuotePos = strSearch.IndexOf('"', intSubStringStart))
                {
                    sbdReturn.Append(strSearch, intSubStringStart, intQuotePos - intSubStringStart)
                             .Append("\", '\"', \"");
                    intSubStringStart = intQuotePos + 1;
                }

                return sbdReturn.Append(strSearch, intSubStringStart, strSearch.Length - intSubStringStart)
                                .Append("\")").ToString();
            }
        }

        /// <summary>
        /// Escapes characters in a string that would cause confusion if the string were placed as HTML content
        /// </summary>
        /// <param name="strToClean">String to clean.</param>
        /// <returns>Copy of <paramref name="strToClean"/> with the characters "&", the greater than sign, and the lesser than sign escaped for HTML.</returns>
        public static string CleanForHtml(this string strToClean)
        {
            if (string.IsNullOrEmpty(strToClean))
                return string.Empty;
            string strReturn = strToClean
                               .Replace("&", "&amp;")
                               .Replace("&amp;amp;", "&amp;")
                               .Replace("<", "&lt;")
                               .Replace(">", "&gt;");
            return s_RgxLineEndingsExpression.Value.Replace(strReturn, "<br />");
        }

        private static readonly ReadOnlyCollection<char> s_achrPathInvalidPathChars
            = Array.AsReadOnly(Path.GetInvalidPathChars());

        private static readonly ReadOnlyCollection<char> s_achrPathInvalidFileNameChars
            = Array.AsReadOnly(Path.GetInvalidFileNameChars());

        /// <summary>
        /// Replaces all the characters in a string that are invalid for file names with underscores.
        /// </summary>
        /// <param name="strToClean">String to clean.</param>
        /// <param name="blnEscapeOnlyPathInvalidChars">If true, only characters that are invalid in path names will be replaced with underscores.</param>
        /// <returns>Copy of <paramref name="strToClean"/> with all characters that are not valid for file names replaced with underscores.</returns>
        public static string CleanForFileName(this string strToClean, bool blnEscapeOnlyPathInvalidChars = false)
        {
            if (string.IsNullOrEmpty(strToClean))
                return string.Empty;
            foreach (char invalidChar in blnEscapeOnlyPathInvalidChars
                         ? s_achrPathInvalidPathChars
                         : s_achrPathInvalidFileNameChars)
                strToClean = strToClean.Replace(invalidChar, '_');
            return strToClean;
        }


        /// <summary>
        /// Strips RTF formatting from a string
        /// </summary>
        /// <param name="strInput">String to process</param>
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns>Version of <paramref name="strInput"/> without RTF formatting codes</returns>
        public static string RtfToPlainText(this string strInput, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(strInput))
                return string.Empty;
            string strInputTrimmed = strInput.TrimStart();
            string strReturn = strInputTrimmed.StartsWith("{/rtf1", StringComparison.Ordinal)
                               || strInputTrimmed.StartsWith(@"{\rtf1", StringComparison.Ordinal)
                ? strInput.StripRichTextFormat(token)
                : strInput;

            return strReturn.NormalizeWhiteSpace();
        }

        /// <summary>
        /// Strips RTF formatting from a string
        /// </summary>
        /// <param name="strInput">String to process</param>
        /// <param name="token">Cancellation token to listen to.</param>
        /// <returns>Version of <paramref name="strInput"/> without RTF formatting codes</returns>
        public static Task<string> RtfToPlainTextAsync(this string strInput, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled<string>(token);
            if (string.IsNullOrEmpty(strInput))
                return Task.FromResult(string.Empty);
            return Task.Run(() =>
            {
                string strInputTrimmed = strInput.TrimStart();
                string strReturn = strInputTrimmed.StartsWith("{/rtf1", StringComparison.Ordinal)
                                   || strInputTrimmed.StartsWith(@"{\rtf1", StringComparison.Ordinal)
                    ? strInput.StripRichTextFormat(token)
                    : strInput;

                return strReturn.NormalizeWhiteSpace();
            }, token);
        }

        public static string RtfToHtml(this string strInput, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(strInput))
                return string.Empty;
            string strReturn = strInput.IsRtf() ? Rtf.ToHtml(strInput) : strInput.CleanForHtml();
            return strReturn.CleanStylisticLigatures().NormalizeWhiteSpace().CleanOfInvalidUnicodeChars();
        }

        public static Task<string> RtfToHtmlAsync(this string strInput, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
                return Task.FromCanceled<string>(token);
            if (string.IsNullOrEmpty(strInput))
                return Task.FromResult(string.Empty);
            return Task.Run(() =>
            {
                string strReturn = strInput.IsRtf()
                    ? Rtf.ToHtml(strInput)
                    : strInput.CleanForHtml();
                return strReturn.CleanStylisticLigatures().NormalizeWhiteSpace().CleanOfInvalidUnicodeChars();
            }, token);
        }

        /// <summary>
        /// Whether a string is an RTF document
        /// </summary>
        /// <param name="strInput">The string to check.</param>
        /// <returns>True if <paramref name="strInput"/> is an RTF document, False otherwise.</returns>
        public static bool IsRtf(this string strInput)
        {
            if (string.IsNullOrEmpty(strInput))
                return false;
            string strInputTrimmed = strInput.TrimStart();
            if (strInputTrimmed.StartsWith("{/rtf1", StringComparison.Ordinal)
                || strInputTrimmed.StartsWith(@"{\rtf1", StringComparison.Ordinal))
            {
                return s_RtfStripperRegex.Value.IsMatch(strInputTrimmed);
            }

            return false;
        }

        /// <summary>
        /// Cleans a string of characters that could cause issues when saved in an xml file and then loaded back in
        /// </summary>
        /// <param name="strInput"></param>
        /// <returns></returns>
        public static string CleanOfInvalidUnicodeChars(this string strInput)
        {
            return string.IsNullOrEmpty(strInput)
                ? string.Empty
                : s_RgxInvalidUnicodeCharsExpression.Value.Replace(strInput, string.Empty);
        }

        private static readonly Lazy<Regex> s_RgxInvalidUnicodeCharsExpression = new Lazy<Regex>(() => new Regex(
            @"[\u0000-\u0008\u000B\u000C\u000E-\u001F]",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled));

        private static readonly Lazy<Regex> s_RgxLineEndingsExpression = new Lazy<Regex>(() => new Regex(@"\r\n|\n\r|\n|\r",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled));

        private static readonly Lazy<Regex> s_RgxEscapedLineEndingsExpression = new Lazy<Regex>(() => new Regex(@"\\r\\n|\\n\\r|\\n|\\r",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled));

        /// <summary>
        /// Strip RTF Tags from RTF Text.
        /// Translated by Chris Benard (with some modifications from Delnar_Ersike) from Python located at:
        /// http://stackoverflow.com/a/188877/448
        /// </summary>
        /// <param name="inputRtf">RTF formatted text</param>
        /// <param name="token">Cancellation token to use (if any).</param>
        /// <returns>Plain text from RTF</returns>
        public static string StripRichTextFormat(this string inputRtf, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(inputRtf))
            {
                return string.Empty;
            }

            Match objMatch = s_RtfStripperRegex.Value.Match(inputRtf);

            if (!objMatch.Success)
            {
                // Didn't match the regex
                return inputRtf;
            }

            Stack<StackEntry> stkGroups = new Stack<StackEntry>();
            bool blnIgnorable = false; // Whether this group (and all inside it) are "ignorable".
            int intUCSkip = 1; // Number of ASCII characters to skip after a unicode character.
            int intCurSkip = 0; // Number of ASCII characters left to skip

            StringBuilder sbdReturn = new StringBuilder();

            {
                for (; objMatch.Success; objMatch = objMatch.NextMatch())
                {
                    token.ThrowIfCancellationRequested();
                    string strBrace = objMatch.Groups[5].Value;

                    if (!string.IsNullOrEmpty(strBrace))
                    {
                        intCurSkip = 0;
                        switch (strBrace[0])
                        {
                            case '{':
                                // Push state
                                stkGroups.Push(new StackEntry(intUCSkip, blnIgnorable));
                                break;
                            case '}':
                            {
                                // Pop state
                                StackEntry entry = stkGroups.Pop();
                                intUCSkip = entry.NumberOfCharactersToSkip;
                                blnIgnorable = entry.Ignorable;
                                break;
                            }
                        }
                    }
                    else
                    {
                        string strCharacter = objMatch.Groups[4].Value;
                        if (!string.IsNullOrEmpty(strCharacter)) // \x (not a letter)
                        {
                            intCurSkip = 0;
                            char chrLoop = strCharacter[0];
                            if (chrLoop == '~')
                            {
                                if (!blnIgnorable)
                                {
                                    sbdReturn.Append('\xA0');
                                }
                            }
                            else if ("{}\\".Contains(chrLoop))
                            {
                                if (!blnIgnorable)
                                {
                                    sbdReturn.Append(strCharacter);
                                }
                            }
                            else if (chrLoop == '*')
                            {
                                blnIgnorable = true;
                            }
                        }
                        else
                        {
                            string strWord = objMatch.Groups[1].Value;
                            if (!string.IsNullOrEmpty(strWord)) // \foo
                            {
                                intCurSkip = 0;
                                if (s_SetRtfDestinations.Contains(strWord))
                                {
                                    blnIgnorable = true;
                                }
                                else if (!blnIgnorable)
                                {
                                    if (s_DicSpecialRtfCharacters.TryGetValue(strWord, out string strValue))
                                    {
                                        sbdReturn.Append(strValue);
                                    }
                                    else
                                    {
                                        string strArg = objMatch.Groups[2].Value;
                                        switch (strWord)
                                        {
                                            case "uc":
                                                intUCSkip = int.Parse(strArg);
                                                break;
                                            case "u":
                                            {
                                                int c = int.Parse(strArg);
                                                if (c < 0)
                                                {
                                                    c += 0x10000;
                                                }

                                                sbdReturn.Append(char.ConvertFromUtf32(c));
                                                intCurSkip = intUCSkip;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                string strHex = objMatch.Groups[3].Value;
                                if (!string.IsNullOrEmpty(strHex)) // \'xx
                                {
                                    if (intCurSkip > 0)
                                    {
                                        --intCurSkip;
                                    }
                                    else if (!blnIgnorable)
                                    {
                                        int c = int.Parse(strHex, System.Globalization.NumberStyles.HexNumber);
                                        sbdReturn.Append(char.ConvertFromUtf32(c));
                                    }
                                }
                                else
                                {
                                    string strTChar = objMatch.Groups[6].Value;
                                    if (!string.IsNullOrEmpty(strTChar))
                                    {
                                        if (intCurSkip > 0)
                                        {
                                            --intCurSkip;
                                        }
                                        else if (!blnIgnorable)
                                        {
                                            sbdReturn.Append(strTChar);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return sbdReturn.ToString();
            }
        }

        private readonly struct StackEntry
        {
            public int NumberOfCharactersToSkip { get; }
            public bool Ignorable { get; }

            public StackEntry(int numberOfCharactersToSkip, bool ignorable)
            {
                NumberOfCharactersToSkip = numberOfCharactersToSkip;
                Ignorable = ignorable;
            }
        }

        private static readonly Lazy<Regex> s_RtfStripperRegex = new Lazy<Regex>(() => new Regex(
            @"\\([a-z]{1,32})(-?\d{1,10})?[ ]?|\\'([0-9a-f]{2})|\\([^a-z])|([{}])|[\r\n]+|(.)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled));

        private static readonly IReadOnlyCollection<string> s_SetRtfDestinations = new HashSet<string>
        {
            "aftncn", "aftnsep", "aftnsepc", "annotation", "atnauthor", "atndate", "atnicn", "atnid",
            "atnparent", "atnref", "atntime", "atrfend", "atrfstart", "author", "background",
            "bkmkend", "bkmkstart", "blipuid", "buptim", "category", "colorschememapping",
            "colortbl", "comment", "company", "creatim", "datafield", "datastore", "defchp", "defpap",
            "do", "doccomm", "docvar", "dptxbxtext", "ebcend", "ebcstart", "factoidname", "falt",
            "fchars", "ffdeftext", "ffentrymcr", "ffexitmcr", "ffformat", "ffhelptext", "ffl",
            "ffname", "ffstattext", "field", "file", "filetbl", "fldinst", "fldrslt", "fldtype",
            "fname", "fontemb", "fontfile", "fonttbl", "footer", "footerf", "footerl", "footerr",
            "footnote", "formfield", "ftncn", "ftnsep", "ftnsepc", "g", "generator", "gridtbl",
            "header", "headerf", "headerl", "headerr", "hl", "hlfr", "hlinkbase", "hlloc", "hlsrc",
            "hsv", "htmltag", "info", "keycode", "keywords", "latentstyles", "lchars", "levelnumbers",
            "leveltext", "lfolevel", "linkval", "list", "listlevel", "listname", "listoverride",
            "listoverridetable", "listpicture", "liststylename", "listtable", "listtext",
            "lsdlockedexcept", "macc", "maccPr", "mailmerge", "maln", "malnScr", "manager", "margPr",
            "mbar", "mbarPr", "mbaseJc", "mbegChr", "mborderBox", "mborderBoxPr", "mbox", "mboxPr",
            "mchr", "mcount", "mctrlPr", "md", "mdeg", "mdegHide", "mden", "mdiff", "mdPr", "me",
            "mendChr", "meqArr", "meqArrPr", "mf", "mfName", "mfPr", "mfunc", "mfuncPr", "mgroupChr",
            "mgroupChrPr", "mgrow", "mhideBot", "mhideLeft", "mhideRight", "mhideTop", "mhtmltag",
            "mlim", "mlimloc", "mlimlow", "mlimlowPr", "mlimupp", "mlimuppPr", "mm", "mmaddfieldname",
            "mmath", "mmathPict", "mmathPr", "mmaxdist", "mmc", "mmcJc", "mmconnectstr",
            "mmconnectstrdata", "mmcPr", "mmcs", "mmdatasource", "mmheadersource", "mmmailsubject",
            "mmodso", "mmodsofilter", "mmodsofldmpdata", "mmodsomappedname", "mmodsoname",
            "mmodsorecipdata", "mmodsosort", "mmodsosrc", "mmodsotable", "mmodsoudl",
            "mmodsoudldata", "mmodsouniquetag", "mmPr", "mmquery", "mmr", "mnary", "mnaryPr",
            "mnoBreak", "mnum", "mobjDist", "moMath", "moMathPara", "moMathParaPr", "mopEmu",
            "mphant", "mphantPr", "mplcHide", "mpos", "mr", "mrad", "mradPr", "mrPr", "msepChr",
            "mshow", "mshp", "msPre", "msPrePr", "msSub", "msSubPr", "msSubSup", "msSubSupPr", "msSup",
            "msSupPr", "mstrikeBLTR", "mstrikeH", "mstrikeTLBR", "mstrikeV", "msub", "msubHide",
            "msup", "msupHide", "mtransp", "mtype", "mvertJc", "mvfmf", "mvfml", "mvtof", "mvtol",
            "mzeroAsc", "mzeroDesc", "mzeroWid", "nesttableprops", "nextfile", "nonesttables",
            "objalias", "objclass", "objdata", "object", "objname", "objsect", "objtime", "oldcprops",
            "oldpprops", "oldsprops", "oldtprops", "oleclsid", "operator", "panose", "password",
            "passwordhash", "pgp", "pgptbl", "picprop", "pict", "pn", "pnseclvl", "pntext", "pntxta",
            "pntxtb", "printim", "private", "propname", "protend", "protstart", "protusertbl", "pxe",
            "result", "revtbl", "revtim", "rsidtbl", "rxe", "shp", "shpgrp", "shpinst",
            "shppict", "shprslt", "shptxt", "sn", "sp", "staticval", "stylesheet", "subject", "sv",
            "svb", "tc", "template", "themedata", "title", "txe", "ud", "upr", "userprops",
            "wgrffmtfilter", "windowcaption", "writereservation", "writereservhash", "xe", "xform",
            "xmlattrname", "xmlattrvalue", "xmlclose", "xmlname", "xmlnstbl",
            "xmlopen"
        };

        private static readonly IReadOnlyDictionary<string, string> s_DicSpecialRtfCharacters = new Dictionary<string, string>
        {
            {"par", "\n"},
            {"sect", "\n\n"},
            {"page", "\n\n"},
            {"line", "\n"},
            {"tab", "\t"},
            {"emdash", "\u2014"},
            {"endash", "\u2013"},
            {"emspace", "\u2003"},
            {"enspace", "\u2002"},
            {"qmspace", "\u2005"},
            {"bullet", "\u2022"},
            {"lquote", "\u2018"},
            {"rquote", "\u2019"},
            {"ldblquote", "\u201C"},
            {"rdblquote", "\u201D"},
        };

        /// <summary>
        /// Converts the specified string, which encodes binary data as base-64 digits, to an equivalent 8-bit unsigned integer array.
        /// Nearly identical to Convert.FromBase64String(), but the byte array that's returned is from a shared ArrayPool instead of newly allocated.
        /// </summary>
        /// <param name="s">The string to convert.</param>
        /// <param name="arrayLength">Actual length of the array used. Important because ArrayPool array can be larger than the lengths requested</param>
        /// <param name="token">Cancellation token to listen to, if any.</param>
        /// <returns>A rented array (from ArrayPool.Shared) of 8-bit unsigned integers that is equivalent to s.</returns>
        /// <exception cref="ArgumentNullException">s is null.</exception>
        /// <exception cref="FormatException">The length of s, ignoring white-space characters, is not zero or a multiple of 4. -or-The format of s is invalid. s contains a non-base-64 character, more than two padding characters, or a non-white space-character among the padding characters.</exception>
        public static byte[] ToBase64PooledByteArray(this string s, out int arrayLength, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(s);

            var array = Convert.FromBase64String(s);
            arrayLength = array.Length;
            var shared = ArrayPool<byte>.Shared.Rent(arrayLength);
            Array.Copy(array, shared, arrayLength);
            return shared;
        }
    }
}
