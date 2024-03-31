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

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Chummer
{

    /// <summary>
    /// Version, despite implementing TryParse, does not implement IParseable.
    /// So we use this struct shim to do it for us. Also allows us to
    /// override TryParse to handle numbers with no decimals.
    /// </summary>
    public readonly struct VersionShim : IParsable<VersionShim>
    {
        private readonly Version? InnerVersion;

        private VersionShim(Version? innerVersion)
        {
            InnerVersion = innerVersion;
        }

        public static implicit operator Version?(VersionShim shim) => shim.InnerVersion;

        public static VersionShim Parse(string s, IFormatProvider? provider)
        {
            return new VersionShim(Version.Parse(s));
        }

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out VersionShim result)
        {
            bool success = TryParse(s, out Version? version);
            result = new VersionShim(version);
            return success;
        }

        public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out Version result)
        {
            if (int.TryParse(s, out int i))
            {
                result = new Version(i, 0);
                return true;
            }

            return Version.TryParse(s, out result);
        }
    }
}
