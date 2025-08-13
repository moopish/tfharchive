using System.Buffers.Binary;
using System.Text;

namespace tfharchive.archive.data
{
    /// <summary>
    /// An entry in the archive file. This will give the information on where the file is located,
    /// the size of the file, the name and directory, and any other potential information that is stored with the file.
    /// </summary>
    /// <param name="Directory">The directory the entry is located in.</param>
    /// <param name="Name">The name of the file.</param>
    /// <param name="Extra">Any extra data that was stored alongside the name.
    /// For example, for images the palette name may be stored with the file name.</param>
    /// <param name="Size">The size of the file.</param>
    /// <param name="Offset">The offset from the start of the file in bytes.</param>
    public sealed record class ArchiveEntry(string Directory, string Name, string Extra, int Size, int Offset)
    {
        private const char NullCharacter = '\0';
        private const char DirectoryDelimiter = '\\';
        private const int FileNameLength = 32;
        private const int FileSizeOffset = FileNameLength;
        private const int FileOffsetOffset = FileSizeOffset + sizeof(int);

        public const int FileEntryLength = FileNameLength + sizeof(int) * 2;

        public byte[] AsBytes()
        {
            int needed = (string.IsNullOrEmpty(Directory) ? 0 : Encoding.ASCII.GetByteCount(Directory) + 1)
                + Encoding.ASCII.GetByteCount(Name)
                + (string.IsNullOrEmpty(Extra) ? 0 : 1 + Encoding.ASCII.GetByteCount(Extra));
            if (needed > FileNameLength) throw new ArgumentException("Name field exceeds 32 bytes.");

            byte[] bytes = new byte[FileEntryLength];
            Span<byte> span = bytes;
            Span<byte> nameField = span[..FileNameLength];

            int written = 0;

            if (!string.IsNullOrEmpty(Directory))
            {
                written += Encoding.ASCII.GetBytes(Directory, nameField[written..]);
                nameField[written++] = (byte)DirectoryDelimiter;
            }

            written += Encoding.ASCII.GetBytes(Name, nameField[written..]);

            if (!string.IsNullOrEmpty(Extra))
            {
                written++;
                written += Encoding.ASCII.GetBytes(Extra, nameField[written..]);
            }

            BinaryPrimitives.WriteInt32LittleEndian(new Span<Byte>(bytes, FileSizeOffset, sizeof(int)), Size);
            BinaryPrimitives.WriteInt32LittleEndian(new Span<Byte>(bytes, FileOffsetOffset, sizeof(int)), Offset);

            return bytes;
        }

        public static ArchiveEntry Parse(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (data.Length < FileEntryLength)
            {
                throw new ArgumentException($"Array must have length {FileEntryLength} bytes.", nameof(data));
            }

            string name = Encoding.ASCII.GetString(data, 0, FileNameLength).TrimEnd(NullCharacter);

            int index = name.IndexOf(DirectoryDelimiter);
            string directory = "";

            if (index != -1)
            {
                directory = name[..index];
                name = name[(index + 1)..];
            }

            string extra = "";

            index = name.IndexOf(NullCharacter);
            if (index != -1)
            {
                extra = name[(index + 1)..];
                name = name[..index];
            }

            int size = BinaryPrimitives.ReadInt32LittleEndian(new Span<Byte>(data, FileSizeOffset, sizeof(int)));
            int entryOffset = BinaryPrimitives.ReadInt32LittleEndian(new Span<Byte>(data, FileOffsetOffset, sizeof(int)));

            return new ArchiveEntry(directory, name, extra, size, entryOffset);
        }
    }
}
