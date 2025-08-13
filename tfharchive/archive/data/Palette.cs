using System.Collections.Immutable;
using System.Text;

namespace tfharchive.archive.data
{
    public class Palette : File
    {
        public const int PaletteSize = 256;
        public const int ByteSize = PaletteSize * 3;

        public const string FileDirectory = "ART";
        public const string FileExtension = "ACT";

        private readonly int[] _colours;

        /// <summary>
        /// Initialize a palette with the given filename and raw data.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="data">The raw byte data of the palette (must be exactly 768 bytes).</param>
        public Palette(string filename, byte[] data) : base(filename)
        {
            if (data == null || data.Length != ByteSize)
            {
                throw new ArgumentException("Palette data must be exactly 768 bytes.", nameof(data));
            }

            var colours = new int[PaletteSize];
            for (int i = 0; i < PaletteSize; i++)
            {
                int r = data[i * 3];
                int g = data[i * 3 + 1];
                int b = data[i * 3 + 2];
                colours[i] = r << 16 | g << 8 | b;
            }
            _colours = colours;
        }

        public override string Directory => FileDirectory;

        /// <summary>
        /// The colours stored in the palette.
        /// </summary>
        public ReadOnlySpan<int> Colours => _colours;

        public override string Extension => FileExtension;

        public override FileType FileType => FileType.Palette;

        public override byte[] AsBytes()
        {
            byte[] bytes = new byte[ByteSize];

            for (int i = 0; i < PaletteSize; ++i)
            {
                int colour = _colours[i];
                bytes[i * 3] = (byte)(colour >> 16 & 0xFF); // red
                bytes[i * 3 + 1] = (byte)(colour >> 8 & 0xFF); // green
                bytes[i * 3 + 2] = (byte)(colour & 0xFF); // blue
            }

            return bytes;
        }

        /// <summary>
        /// Serializes the palette colours in the format: "(r0,g0,b0),(r1,g1,b1),...,(r255,g255,b255)".
        /// </summary>
        /// <returns>The palette represented as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < _colours.Length; i++)
            {
                int color = _colours[i];
                int r = color >> 16 & 0xFF;
                int g = color >> 8 & 0xFF;
                int b = color & 0xFF;
                sb.Append($"({r},{g},{b})");
                if (i < _colours.Length - 1) sb.Append(',');
            }
            return sb.ToString();
        }
    }
}
