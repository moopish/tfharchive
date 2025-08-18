namespace tfharchive.video
{
    /// <summary>
    /// Represents a block of the PCM audio data that is played during a TVI.
    /// </summary>
    public class TVIAudioFrame
    {
        /// <summary>
        /// The marker in the block to notify that the block is related to audio.
        /// </summary>
        private const int AudioBlock = 0x03;

        /// <summary>
        /// The audio sample rate for a TVI video file.
        /// </summary>
        public const int SampleRateHz = 11025;

        public ReadOnlyMemory<byte> AudioData { get; }

        /// <summary>
        /// How long the segment of audio is in milliseconds.
        /// </summary>
        public readonly int Duration;

        /// <summary>
        /// The associated frame number of the audio block.
        /// </summary>
        public readonly int FrameNumber;

        /// <summary>
        /// Initialize the audio frame given the frame number, duration in milliseconds, and frame data.
        /// </summary>
        /// <param name="frameNumber">The video frame number that the audio was stored before.</param>
        /// <param name="milliseconds">The duration of the clip in milliseconds</param>
        /// <param name="data">The PCM audio data.</param>
        private TVIAudioFrame(int frameNumber, int milliseconds, ReadOnlyMemory<byte> data) 
        { 
            FrameNumber = frameNumber;
            Duration = milliseconds;
            AudioData = data;
        }

        /// <summary>
        /// Parse the given data to make it an acceptable TVIAudioFrame.
        /// </summary>
        /// <param name="frameNumber">The video frame number that the audio was stored before.</param>
        /// <param name="data"></param>
        /// <returns>The unparsed PCM audio data.</returns>
        public static TVIAudioFrame Parse(int frameNumber, ReadOnlyMemory<byte> data)
        {
            data = data[1..]; // Remove chunk identifier
            return new TVIAudioFrame(frameNumber, data.Length * 1000 / SampleRateHz, data);
        }
    }
}
