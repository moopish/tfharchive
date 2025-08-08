using System.Text;

namespace tfharchive.archive
{
    public class Image : File
    {
        public const string FileDirectory = "ART";
        public const string FileExtention = "RAW";

        private readonly byte[,] _pixels;
        private readonly int _width;
        private readonly int _height;

        /// <summary>
        /// Initialize an image with the given filename and raw data.
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <param name="data">The raw byte data of the image.</param>
        public Image(string filename, byte[] data) : base(filename)
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

            // Store pixels in a 2D array (row-major)
            _pixels = new byte[_height, _width];
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _pixels[y, x] = data[(y * _width) + x];
                }
            }
        }

        public override string Directory => FileDirectory;

        public override string Extension => FileExtention;

        public override byte[] AsBytes()
        {
            byte[] bytes = new byte[_width * _height];

            for (int y = 0; y < _height; ++y)
            {
                for (int x = 0;  x < _width; ++x)
                {
                    bytes[y * _width + x] = _pixels[y, x];
                }
            }

            return bytes;
        }

        /// <summary>
        /// Retrieve the pixel at the given (x,y) location.
        /// </summary>
        /// <param name="x">The column of the pixel.</param>
        /// <param name="y">The row of the pixel.</param>
        /// <returns>The pixel (the index used with the palette to get the colour).</returns>
        public int PixelAt(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                throw new ArgumentOutOfRangeException($"Pixel coordinates ({x}, {y}) are out of bounds for image size {_width}x{_height}.");
            }
            return _pixels[y, x];
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
            switch (size)
            {
                case 4096: return (64, 64);
                case 65536: return (256, 256);
                default: throw new ArgumentOutOfRangeException(nameof(size) + " -> " + size);
           }
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
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (sb.Length > 0) sb.Append(',');
                    sb.Append(_pixels[y, x]);
                }
            }
            return sb.ToString();
        }
    }
}
