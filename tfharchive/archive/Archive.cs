using System.Collections.Immutable;
using System.Text;
using System.Xml.Linq;

namespace tfharchive.archive
{
    public class Archive
    {
        private readonly long _archiveSize;
        private readonly string _description;
        private readonly string _filepath;
        private readonly List<(string directory, string name, string extra, int size, int offset)> _files;

        /// <summary>
        /// Private constructor to enforce loading via static Load method
        /// </summary>
        /// <param name="description">The archive description.</param>
        /// <param name="files">The file listing.</param>
        /// <param name="filepath">The path to the file archive for later access.</param>
        /// <param name="archiveSize">The size of the archive in bytes for validation.</param>
        private Archive(string description, List<(string, string, string, int, int)> files, string filepath, long archiveSize)
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
        public int FileCount => _files.Count;

        /// <summary>
        /// Check if a given file name exists in the archive.
        /// </summary>
        /// <param name="filename">The file name to check for.</param>
        /// <returns>True if it exists, false otherwise.</returns>
        public bool Contains(string filename) => _files.Any(f => string.Equals(f.name, filename, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Get the names of the files that are in the archive sorted alphabetically.
        /// </summary>
        /// <returns>A list of the file names.</returns>
        public ImmutableList<string> FileNames() => [.. _files.Select(static f => f.name).OrderBy(static name => name)];

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
                if (name != null && name.EndsWith($".{Image.FileExtention}", StringComparison.OrdinalIgnoreCase)
                    && directory == Image.FileDirectory && IsValidEntry(offset, size))
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    byte[] data = new byte[size];
                    stream.ReadExactly(data, 0, size);
                    images.Add(new Image(name, data));
                }
            }
            return images;
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
                var (directory, name, extra, size, offset) = _files.FirstOrDefault(f => string.Equals(f.name, filename, StringComparison.OrdinalIgnoreCase));
                if (name != null && name.EndsWith($".{Image.FileExtention}", StringComparison.OrdinalIgnoreCase)
                    && name.StartsWith($"{Image.FileDirectory}/", StringComparison.OrdinalIgnoreCase) && IsValidEntry(offset, size))
                {
                    stream.Seek(offset, SeekOrigin.Begin);
                    byte[] data = new byte[size];
                    stream.ReadExactly(data, 0, size);
                    images.Add(new Image(name, data));
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
                var (directory, name, extra, size, offset) = _files.FirstOrDefault(f => string.Equals(f.name, filename, StringComparison.OrdinalIgnoreCase));
                if (name != null && name.EndsWith($".{Palette.FileExtention}", StringComparison.OrdinalIgnoreCase)
                    && name.StartsWith($"{Palette.FileDirectory}/", StringComparison.OrdinalIgnoreCase) && IsValidEntry(offset, size))
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
            byte[] descBytes = reader.ReadBytes(80);
            string description = Encoding.ASCII.GetString(descBytes).TrimEnd('\0');

            // Read file entries
            var files = new List<(string directory, string name, string extra, int size, int offset)>(fileCount);
            for (int i = 0; i < fileCount; i++)
            {
                byte[] nameBytes = reader.ReadBytes(32);
                string name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
                string directory = name[..name.IndexOf('\\')];
                name = name[(name.IndexOf('\\') + 1)..];

                string extra = "";

                if (name.Contains('\0'))
                {
                    extra = name[(name.IndexOf('\0') + 1)..];
                    name = name[..name.IndexOf('\0')];
                }

                int size = reader.ReadInt32();
                int offset = reader.ReadInt32();
                files.Add((directory, name, extra, size, offset));
            }

            // Validate that offsets point within the data section
            int expectedMinOffset = 4 + 80 + (fileCount * (32 + 4 + 4)); // Header + desc + entries
            if (files.Any(f => f.offset < expectedMinOffset || f.offset + f.size > archiveSize))
            {
                throw new InvalidDataException("Invalid file offset or size in archive.");
            }

            return new Archive(description, files, filepath, archiveSize);
        }
    }
}
