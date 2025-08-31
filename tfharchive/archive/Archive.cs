using System.Collections.Immutable;
using System.IO;
using System.Text;
using tfharchive.archive.data;
using tfharchive.archive.data.art;
using tfharchive.archive.data.startup;

namespace tfharchive.archive
{
    public class Archive
    {
        private const char NullCharacter = '\0';
        private const int DescriptionHeaderLength = 80;

        private readonly long _archiveSize;
        private readonly string _description;
        private readonly string _filepath;
        private readonly ArchiveEntry[] _files;

        /// <summary>
        /// Private constructor to enforce loading via static Load method
        /// </summary>
        /// <param name="description">The archive description.</param>
        /// <param name="files">The file listing.</param>
        /// <param name="filepath">The path to the file archive for later access.</param>
        /// <param name="archiveSize">The size of the archive in bytes for validation.</param>
        private Archive(string description, ArchiveEntry[] files, string filepath, long archiveSize)
        {
            _description = description;
            _files = files;
            _filepath = filepath;
            _archiveSize = archiveSize;
        }

        /// <summary>
        /// The description of the archive.
        /// Each archive has a short string that gives some information about the archive.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Get the number of files in the Archive.
        /// </summary>
        public int FileCount => _files.Length;

        /// <summary>
        /// Check if a given file name exists in the archive.
        /// </summary>
        /// <param name="filename">The file name to check for.</param>
        /// <returns>True if it exists, false otherwise.</returns>
        public bool Contains(string filename) => _files.Any(f => string.Equals(f.Name, filename, StringComparison.OrdinalIgnoreCase));

        public bool DumpRawFile(string filepath, string fileDirectory, string filename)
        {
            using var stream = new FileStream(_filepath, FileMode.Open, FileAccess.Read);
            foreach ((string directory, string name, string extra, int size, int offset) in _files)
            {
                if (name == filename && directory == fileDirectory && IsValidEntry(offset, size))
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    byte[] data = new byte[size];
                    stream.ReadExactly(data, 0, size);
                    System.IO.File.WriteAllBytes(filepath, data);
                    stream.Close();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The entries of the archive.
        /// </summary>
        public IEnumerable<ArchiveEntry> Entries => _files;


        /// <summary>
        /// Get the names of the files that are in the archive sorted alphabetically.
        /// </summary>
        /// <returns>A list of the file names.</returns>
        public ImmutableList<string> FileNames() => [.. _files.Select(static f => f.Name).OrderBy(static name => name)];

        /// <summary>
        /// Gets all the images from the archive
        /// </summary>
        /// <returns>A list of the images in the archive.</returns>
        public List<Image> GetAllImages()
        {
            using var stream = new FileStream(_filepath, FileMode.Open, FileAccess.Read);
            var images = new List<Image>();
            foreach ((string directory, string name, string extra, int size, int offset) in _files)
            {
                if (name != null && name.EndsWith($".{Image.FileExtension}", StringComparison.OrdinalIgnoreCase)
                    && directory == Image.FileDirectory && IsValidEntry(offset, size))
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    byte[] data = new byte[size];
                    stream.ReadExactly(data, 0, size);
                    images.Add(new Image(name, extra, data));
                }
            }
            return images;
        }

        public EndScreen? GetEndScreen(string filename)
        {
            using var stream = new FileStream(_filepath, FileMode.Open, FileAccess.Read);
            EndScreen? endScreen = null;
            foreach ((string directory, string name, string extra, int size, int offset) in _files)
            {
                if (name.Equals(filename, StringComparison.OrdinalIgnoreCase) && name.EndsWith($".{EndScreen.FileExtension}", StringComparison.OrdinalIgnoreCase)
                    && directory == EndScreen.FileDirectory && IsValidEntry(offset, size))
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    byte[] data = new byte[size];
                    stream.ReadExactly(data, 0, size);
                    endScreen = new EndScreen(name, data);
                    break;
                }
            }
            return endScreen;
        }

        /// <summary>
        /// Gets the image files with the given file names.
        /// </summary>
        /// <param name="filenames">The file names of the images to get.</param>
        /// <returns>A list of the requested images.</returns>
        public List<Image> GetImages(params string[] filenames)
        {
            using var stream = new FileStream(_filepath, FileMode.Open, FileAccess.Read);
            var images = new List<Image>();
            foreach (var filename in filenames)
            {
                var (directory, name, extra, size, offset) = _files.FirstOrDefault(f => string.Equals(f.Name, filename, StringComparison.OrdinalIgnoreCase));
                if (name != null && name.EndsWith($".{Image.FileExtension}", StringComparison.OrdinalIgnoreCase)
                    && directory == Image.FileDirectory && IsValidEntry(offset, size))
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    byte[] data = new byte[size];
                    stream.ReadExactly(data, 0, size);
                    images.Add(new Image(name, extra, data));
                }
            }
            return images;
        }

        /// <summary>
        /// Gets the palette files with the given file names.
        /// </summary>
        /// <param name="filenames">The file names of the palettes to get.</param>
        /// <returns>A list of the requested palettes.</returns>
        public List<Palette> GetPalettes(params string[] filenames)
        {
            using var stream = new FileStream(_filepath, FileMode.Open, FileAccess.Read);
            var palettes = new List<Palette>();
            foreach (var filename in filenames)
            {
                var (directory, name, extra, size, offset) = _files.FirstOrDefault(f => string.Equals(f.Name, filename, StringComparison.OrdinalIgnoreCase));
                if (name != null && name.EndsWith($".{Palette.FileExtension}", StringComparison.OrdinalIgnoreCase)
                    && directory == Palette.FileDirectory && IsValidEntry(offset, size))
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    byte[] data = new byte[size];
                    stream.ReadExactly(data, 0, size);
                    palettes.Add(new Palette(name, data));
                }
            }
            return palettes;
        }

        /// <summary>
        /// TODO delete this, quick function for testing only
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public TextFile GetRawFile(string directory, string name)
        {
            using var stream = new FileStream(_filepath, FileMode.Open, FileAccess.Read);
            TextFile textFile = null;
            var (eDirectory, eEame, extra, size, offset) = _files.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
            if (name != null && directory == eDirectory && IsValidEntry(offset, size))
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    byte[] data = new byte[size];
                    stream.ReadExactly(data, 0, size);
                    textFile = new TextFile(name, data);
               }
            return textFile;
        }

        /// <summary>
        /// Helper to validate entry extraction.
        /// </summary>
        /// <param name="offset">The entry's offset.</param>
        /// <param name="size">The entry's file size in bytes.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private bool IsValidEntry(int offset, int size) => offset >= 0 && size >= 0 && offset + size <= _archiveSize;

        /// <summary>
        /// Given a path, open and extract from the file the file count, description,
        /// file names, file sizes, and file offsets and store them in the Archive
        /// instance that is returned.
        /// </summary>
        /// <param name="filepath">The relative or absolute path to the archive.</param>
        /// <returns>An Archive instance representing the archive found at the given path.</returns>
        public static Archive Load(string filepath)
        {
            if (string.IsNullOrEmpty(filepath) || !System.IO.File.Exists(filepath))
            {
                throw new FileNotFoundException("Archive file not found or invalid path.", filepath);
            }

            long archiveSize = new FileInfo(filepath).Length;

            using var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            // Read file count (32-bit int)
            int fileCount = reader.ReadInt32();
                
            // Read description (80 ASCII bytes, trim nulls)
            byte[] descBytes = reader.ReadBytes(DescriptionHeaderLength);
            string description = Encoding.ASCII.GetString(descBytes).TrimEnd(NullCharacter);

            // Read file entries
            var files = new List<ArchiveEntry>(fileCount);
            for (int i = 0; i < fileCount; i++)
            {
                files.Add(ArchiveEntry.Parse(reader.ReadBytes(ArchiveEntry.FileEntryLength)));
            }

            // Validate that offsets point within the data section
            int expectedMinOffset = 4 + DescriptionHeaderLength + (fileCount * ArchiveEntry.FileEntryLength); // Header + desc + entries
            if (files.Any(f => f.Offset < expectedMinOffset || f.Offset + f.Size > archiveSize))
            {
                throw new InvalidDataException("Invalid file offset or size in archive.");
            }

            return new Archive(description, [.. files], filepath, archiveSize);
        }
    }
}
