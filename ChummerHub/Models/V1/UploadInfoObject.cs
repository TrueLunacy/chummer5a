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
using System.Collections.Generic;

namespace ChummerHub.Models.V1
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UploadInfoObject'
    public class UploadInfoObject
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UploadInfoObject'
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UploadInfoObject.UploadDateTime'
        public DateTime? UploadDateTime { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UploadInfoObject.UploadDateTime'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UploadInfoObject.Client'
        public UploadClient Client { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UploadInfoObject.Client'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UploadInfoObject.SINners'
        public IEnumerable<SINner> SINners { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UploadInfoObject.SINners'

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member 'UploadInfoObject.Groupname'
        public String Groupname { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member 'UploadInfoObject.Groupname'
    }
}
