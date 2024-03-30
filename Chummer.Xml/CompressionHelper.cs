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
using SharpCompress.Compressors.LZMA;
using System.Buffers.Binary;
using System.IO;
using System.Threading.Tasks;

namespace Chummer.Xml
{
    public static class CompressionHelper
    {
        public enum ChummerCompressionPreset
        {
            Fast,
            Balanced,
            Thorough
        }

        public static void CompressToLzmaFile(this Stream input, FileStream output,
                                              ChummerCompressionPreset eChummerCompressionPreset)
        {
            using LzmaStream lzmastream = new LzmaStream(LzmaEncoderProperties.Default, false, output);
            // we have to write the header (properties + length) manually
            output.Write(lzmastream.Properties);
            byte[] length = new byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(length, input.Length);
            output.Write(length);
            input.CopyTo(lzmastream);
        }

        public static async Task CompressToLzmaFileAsync(this Stream objInStream, FileStream objOutStream,
                                                   ChummerCompressionPreset eChummerCompressionPreset)
        {
            CompressToLzmaFile(objInStream, objOutStream, eChummerCompressionPreset);
        }

        public static void DecompressLzmaFile(this FileStream input, Stream output)
        {
            // First 5 bytes contain the lzma 'properties', needed for decoding, why the stream doesn't do this automatically is beyond me
            byte[] properties = new byte[5];
            input.Read(properties, 0, 5);
            // Next 8 bytes is the length
            byte[] length = new byte[8];
            input.Read(length, 0, 8);
            long lengthActual = BinaryPrimitives.ReadInt64LittleEndian(length);
            // lzmastream is reading without the header, so subtract the 13 bytes
            using LzmaStream lzmaStream = new LzmaStream(properties, input, input.Length - 13, lengthActual);
            lzmaStream.CopyTo(output);
        }

        public static async Task DecompressLzmaFileAsync(this FileStream objInStream, Stream objOutStream)
        {
            DecompressLzmaFile(objInStream, objOutStream);
        }
    }
}
