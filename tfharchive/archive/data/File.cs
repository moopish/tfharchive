namespace tfharchive.archive.data
{
    /// <summary>
    /// Set up a file with the given name.
    /// </summary>
    /// <param name="filename">The name of the file.</param>
    public abstract class File(string filename)
    {
        /// <summary>
        /// The directory where the file will be located.
        /// </summary>
        public abstract string Directory {  get; }  

        /// <summary>
        /// The extension of the file.
        /// </summary>
        public abstract string Extension { get; }

        /// <summary>
        /// The file type of the class.
        /// </summary>
        public virtual FileType FileType => FileType.Unknown;

        /// <summary>
        /// The name of the file.
        /// </summary>
        public string Name => filename ?? throw new ArgumentNullException(nameof(filename));

        /// <summary>
        /// Get the file as a byte array as it would appear in the file.
        /// </summary>
        /// <returns>The byte array.</returns>
        public abstract byte[] AsBytes();
    }
}
