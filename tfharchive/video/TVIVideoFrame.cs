using tfharchive.archive.data;

namespace tfharchive.video
{
    /// <summary>
    /// Represents a single video frame from a TVI video file.
    /// </summary>
    public class TVIVideoFrame
    {
        /// <summary>
        /// The side length of the blocks.
        /// </summary>
        private const int BlockSideLength = 8;

        /// <summary>
        /// The area of the blocks. The number of pixels a block has.
        /// </summary>
        private const int BlockSize = BlockSideLength * BlockSideLength;

        /// <summary>
        /// The byte that signifies that the coming block is a compressed block.
        /// </summary>
        private const byte CompressedBlock = 0x04;

        /// <summary>
        /// The number of colours stored in the colour table of the compressed block.
        /// </summary>
        private const int CompressedBlockColourTableLength = 8;

        /// <summary>
        /// The size of a compressed block in bytes.
        /// </summary>
        private const int CompressedBlockSize = 32;

        /// <summary>
        /// The number of bytes for each row in a compressed block.
        /// </summary>
        private const int CompressedRowLength = 3;

        /// <summary>
        /// The byte that signifies that the current block is not different from 
        /// the block in the same location as the last frame.
        /// </summary>
        private const byte EmptyBlock = 0x00;

        /// <summary>
        /// The byte that signifies that the current block uses RLE encoding.
        /// </summary>
        private const byte RLEBlock = 0x02;

        /// <summary>
        /// The byte that signifies that the next two bytes represent a RLE encoding.
        /// In other words: FF x y, where x is the number of pixels and y is the colour
        /// index in the palette to use.
        /// </summary>
        private const byte RLEUsed = 0xFF;


        /// <summary>
        /// The height of the image in pixels.
        /// </summary>
        public const int Height = 120;

        /// <summary>
        /// The width of the image in pixels.
        /// </summary>
        public const int Width = 320;


        /// <summary>
        /// The data of the frame. It stores the indices that are 
        /// used to get the pixel's colour from the palette.
        /// </summary>
        private readonly byte[] _data;


        /// <summary>
        /// The frame number of the frame.
        /// </summary>
        public readonly int FrameNumber;


        /// <summary>
        /// Creates a new video frame.
        /// </summary>
        /// <param name="frameNumber">The frame number.</param>
        /// <param name="data">The frame data.</param>
        private TVIVideoFrame(int frameNumber, byte[] data)
        {
            FrameNumber = frameNumber;
            _data = data;
        }

        /// <summary>
        /// Parses a given compressed block and updates the video frame. 
        /// A compressed block will always be 32 bytes. The first 8 bytes
        /// are colour indices of the palette. The remaining 24 bytes are
        /// used to represent the index of the byte from the colour indices
        /// to use for a given pixel. Each row is represented in 3 bytes.
        /// The index to colour index is stored as 3 bits, each from one of 
        /// the three bytes (all in the same position in the byte).
        /// The least significant bit comes first.
        /// </summary>
        /// <param name="frameData">The video frame data to update.</param>
        /// <param name="compressedBlock">The compressed 32 byte block.</param>
        /// <param name="x">The x-value of the position of the upper left corner of the block in the frame.</param>
        /// <param name="y">The y-value of the position of the upper left corner of the block in the frame.</param>
        private static void CompressedBlockParse(byte[] frameData, byte[] compressedBlock, int x, int y)
        {
            int compressedBlockIndex = CompressedBlockColourTableLength;
            int rowIndex = 0;

            for (int row = 0; row < BlockSideLength; ++row)
            {
                int mask = 1 << (BlockSideLength - 1);
                int frameIndex = x + (y + row) * Width;

                byte lowest = compressedBlock[compressedBlockIndex];
                byte middle = compressedBlock[compressedBlockIndex + 1];
                byte highest = compressedBlock[compressedBlockIndex + 2];

                for (int col = 0; col < BlockSideLength; ++col)
                {
                    int colourIndex = ((lowest & mask) != 0 ? 1 : 0)
                        | ((middle & mask) != 0 ? 2 : 0)
                        | ((highest & mask) != 0 ? 4 : 0);

                    frameData[frameIndex++] = compressedBlock[colourIndex];
                    mask >>= 1;
                }

                rowIndex += CompressedRowLength;
                compressedBlockIndex += 3;
            }
        }

        /// <summary>
        /// Copies a block from a previous frame to the next frame. This occurs when
        /// the block is marked with 0x00.
        /// </summary>
        /// <param name="source">The previous frame data.</param>
        /// <param name="destination">The next frame data.</param>
        /// <param name="x">The x-value of the position of the upper left corner of the block in the frame.</param>
        /// <param name="y">The y-value of the position of the upper left corner of the block in the frame.</param>
        private static void CopyBlock(byte[] source, byte[] destination, int x, int y)
        {
            for (int j = 0; j < BlockSideLength; j++)
            {
                int start = (y + j) * Width + x;
                source.AsSpan(start, BlockSideLength)
                      .CopyTo(destination.AsSpan(start, BlockSideLength));
            }
        }

        /// <summary>
        /// Get an empty frame. The image should be black. Should be used as the frame that is initially passed to Parse.
        /// </summary>
        /// <returns>A frame that is a black image.</returns>
        internal static TVIVideoFrame GetEmptyFrame()
        {
            byte[] frameData = new byte[Width * Height];
            return new TVIVideoFrame(-1, frameData);
        }

        /// <summary>
        /// Get the frame as a byte array that has the bytes for the 
        /// pixel colours of the frame. They are in the order RGBA.
        /// </summary>
        /// <param name="palette">The palette to get the pixel colours from.</param>
        /// <returns>The byte array of the image.</returns>
        public ReadOnlySpan<byte> GetPixelBytes(Palette palette) 
        {
            byte[] pixels = new byte[Width * Height * 4];

            int index = 0;
            foreach (byte b in _data)
            {
                int pixelColour = palette.Colours[b];
                pixels[index] = (byte)(pixelColour & 0xFF);
                pixels[index + 1] = (byte)((pixelColour >> 8) & 0xFF);
                pixels[index + 2] = (byte)((pixelColour >> 16) & 0xFF);
                pixels[index + 3] = 0xFF;
                index += 4;
            }

            return pixels;
        }

        /// <summary>
        /// Parse the frame from the given frame data.
        /// </summary>
        /// <param name="frameNumber">The frame number.</param>
        /// <param name="data">The frame data to parse.</param>
        /// <param name="previousFrame">The previous frame for when a block is a 'no-update' block.</param>
        /// <returns></returns>
        public static TVIVideoFrame Parse(int frameNumber, byte[] data, TVIVideoFrame? previousFrame = null)
        {
            byte[] frameData = new byte[Width * Height];
            BinaryReader reader = new(new MemoryStream(data));
            previousFrame ??= GetEmptyFrame();

            for (int y = 0, x = 0; ; x += BlockSideLength)
            {
                if (x == Width)
                {
                    x = 0;
                    y += BlockSideLength;
                    if (y == Height) break;
                }

                byte blockType = reader.ReadByte();

                switch (blockType)
                {
                    case EmptyBlock:
                        if (previousFrame == null) break;
                        CopyBlock(previousFrame._data, frameData, x, y);
                        break;

                    case RLEBlock:
                        RLEBlockParse(frameData, reader, x, y);
                        break;

                    case CompressedBlock:
                        CompressedBlockParse(frameData, reader.ReadBytes(CompressedBlockSize), x, y);
                        break;
                }
            }

            return new TVIVideoFrame(frameNumber, frameData);
        }

        /// <summary>
        /// Parses blocks that start with a 0x02 byte. These blocks are encoded using a RLE (run-length encoding) method.
        /// The block is an unspecified length and must be read byte by byte. If we find a byte that is equal to 0xFF then
        /// the next two bytes are 'x' the number of pixels to set and 'y' the index of the colour in the palette (FF x y).
        /// Otherwise, it is just the index in the palette.
        /// </summary>
        /// <param name="frameData">The frame that we are currently creating.</param>
        /// <param name="reader">The reader to get the data from.</param>
        /// <param name="x">The x-value of the position of the upper left corner of the block in the frame.</param>
        /// <param name="y">The y-value of the position of the upper left corner of the block in the frame.</param>
        private static void RLEBlockParse(byte[] frameData, BinaryReader reader, int x, int y)
        {
            int i = 0, j = 0;
            byte colour = 0;
            byte remaining = 0;

            while (j != BlockSideLength)
            {
                int index = x + i + (y + j) * Width;
                if (remaining > 0)
                {
                    --remaining;
                }
                else
                {
                    byte subType = reader.ReadByte();

                    if (subType == RLEUsed)
                    {
                        remaining = (byte)(reader.ReadByte() - 1);
                        colour = reader.ReadByte();
                    }
                    else
                    {
                        colour = subType;
                    }
                }
                frameData[index] = colour;

                ++i;
                if (i == BlockSideLength)
                {
                    i = 0;
                    ++j;
                }
            }
        }
    }
}
