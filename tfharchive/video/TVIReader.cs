namespace tfharchive.video
{
    /// <summary>
    /// Reader to read a TVI file. It parses and gives the video frames and audio.
    /// </summary>
    public class TVIReader : IDisposable
    {
        /// <summary>
        /// The current frame, or the previously parsed, frame of the video.
        /// </summary>
        private TVIVideoFrame? _currentFrame;

        /// <summary>
        /// The current frame number.
        /// </summary>
        private int _currentFrameNumber = 0;

        /// <summary>
        /// The reader used to parse the file.
        /// </summary>
        private readonly BinaryReader _reader;

        /// <summary>
        /// The path to the file that was opened.
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// The number of frames in the video.
        /// </summary>
        public readonly int FrameCount;

        /// <summary>
        /// Opens the given TVI file for reading.
        /// </summary>
        /// <param name="filePath">The path to the TVI file.</param>
        public TVIReader(string filePath)
        {
            FilePath = filePath;
            _reader = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read));
            FrameCount = _reader.ReadInt32();
            _reader.BaseStream.Seek(sizeof(int) * FrameCount, SeekOrigin.Current);
        }

        public void Dispose()
        {
            _reader.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Checks if the reader has more frames to read.
        /// </summary>
        /// <returns>True if there are more frames to read, false otherwise.</returns>
        public bool HasNextFrame() => _reader.BaseStream.Position < _reader.BaseStream.Length;

        /// <summary>
        /// Read the next video and audio (if available) frames.
        /// </summary>
        /// <returns>The next video frame and audio frame (if available).</returns>
        public (TVIVideoFrame, TVIAudioFrame?) NextFrame()
        {
            if (!HasNextFrame()) return (TVIVideoFrame.GetEmptyFrame(), null);

            TVIAudioFrame? audioFrame = null;
            TVIVideoFrame videoFrame;
            int frameLength;
            byte[] frameBytes;

            frameLength = _reader.ReadInt32();
            frameBytes = _reader.ReadBytes(frameLength);

            if (frameBytes[0] == 0x03)
            {
                // Audio frame
                audioFrame = TVIAudioFrame.Parse(_currentFrameNumber, frameBytes);
                frameLength = _reader.ReadInt32();
                frameBytes = _reader.ReadBytes(frameLength);
            }

            videoFrame = TVIVideoFrame.Parse(_currentFrameNumber, frameBytes, _currentFrame);

            _currentFrameNumber++;
            _currentFrame = videoFrame;

            return (videoFrame, audioFrame);
        }
    }
}
