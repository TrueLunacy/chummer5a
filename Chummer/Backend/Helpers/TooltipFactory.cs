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

namespace Chummer
{
    public static class ToolTipFactory
    {
        [System.CLSCompliant(false)]
        public static ToolTip ToolTip => new ToolTip
        {
            AutoPopDelay = 3600000,
            InitialDelay = 250,
            IsBalloon = false,
            ReshowDelay = 100,
        };

        public static void SetToolTip(this Control c, string caption)
        {
            c.DoThreadSafe(x => ToolTip.SetToolTip(x, caption));
        }

        public static Task SetToolTipAsync(this Control c, string caption, CancellationToken token = default)
        {
            return c.DoThreadSafeAsync(x => ToolTip.SetToolTip(x, caption), token);
        }
    }
}
