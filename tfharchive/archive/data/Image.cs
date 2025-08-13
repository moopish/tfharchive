using System.Text;

namespace tfharchive.archive.data
{
    public class Image : File
    {
        public const string FileDirectory = "ART";
        public const string FileExtension = "RAW";

        private readonly byte[] _pixels;
        private readonly string _palette;
        private readonly int _width;
        private readonly int _height;

        /// <summary>
        /// Initialize an image with the given filename and raw data.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="data">The raw byte data of the image.</param>
        public Image(string filename, string palette, byte[] data) : base(filename)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Image data cannot be null or empty.", nameof(data));
            }

            // Determine dimensions based on specifications
            int length = data.Length;
            (_width, _height) = SizeToDimension(length);

            // Validate that data matches dimensions
            if (_width * _height != length)
            {
                throw new ArgumentException($"Data length {length} does not match dimensions {_width}x{_height}.", nameof(data));
            }

            _pixels = (byte[])data.Clone();
            _palette = palette;
        }

        public override string Directory => FileDirectory;

        public override string Extension => FileExtension;

        public override FileType FileType => FileType.Image;

        public override byte[] AsBytes()
        {
            return (byte[])_pixels.Clone();
        }

        /// <summary>
        /// Retrieve the pixel at the given (x,y) location.
        /// </summary>
        /// <param name="x">The column of the pixel.</param>
        /// <param name="y">The row of the pixel.</param>
        /// <returns>The pixel (the index used with the palette to get the colour).</returns>
        public byte PixelAt(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                throw new ArgumentOutOfRangeException($"Pixel coordinates ({x}, {y}) are out of bounds for image size {_width}x{_height}.");
            }
            return _pixels[y * _height + x];
        }

        /// <summary>
        /// Determines the dimensions of an image given the number of pixels it has.
        /// </summary>
        /// <param name="size">The number of pixels the image has.</param>
        /// <returns>The dimensions (width, height) of the image.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the size is not in the switch statement it will throw this.</exception>
        private static (int, int) SizeToDimension(int size)
        {
            // TODO F3 and HB have stranger sizes
            return size switch
            {
                4096 => (64, 64),
                65536 => (256, 256),
                _ => throw new ArgumentOutOfRangeException(nameof(size) + " -> " + size),
            };
        }

        /// <summary>
        /// Serializes the image data in the following format: "p1,p2,p3,p4,...,pn"
        /// where n is the number of pixels, and p1 is the top-left and pn is the lower-right pixel.
        /// The pixels should be from left to right, then top to bottom.
        /// </summary>
        /// <returns>A serialized version of the image.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendJoin(',', _pixels);
            return sb.ToString();
        }
    }
}
