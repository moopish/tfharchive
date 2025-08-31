using System.Text;
using tfharchive.utilities.font;

namespace tfharchive.archive.data.startup
{
    public class EndScreen : File
    {
        private const int CharacterTupleByteLength = 2;
        private const int NibbleBitLength = 4;
        private const int NibbleBitMask = 0x0F;

        public const string FileDirectory = "STARTUP";
        public const string FileExtension = "BIN";

        private const int EndScreenLineLength = 80;

        public const int EndScreenByteLength = 4000;

        /// <summary>
        /// Tracks the character, foreground, and background colour of what to print to screen.
        /// </summary>
        /// <param name="Character">The character to print.</param>
        /// <param name="Foreground">The colour of the character.</param>
        /// <param name="Background">The colour of the background where the character is drawn.</param>
        private record struct CharacterTuple(char Character, int Foreground, int Background) { }

        private readonly CharacterTuple[] _characters;

        /// <summary>
        /// Setup a given end screen with the given data.
        /// </summary>
        /// <param name="filename">The file name the end screen is found in.</param>
        /// <param name="data">The data of the end screen file in bytes.</param>
        public EndScreen(string filename, byte[] data) : base(filename)
        {
            CharacterTuple[] characterTuples = new CharacterTuple[EndScreenByteLength / CharacterTupleByteLength];

            for (int i = 0; i < EndScreenByteLength; i += CharacterTupleByteLength)
            {
                int colourByte = data[i + 1];
                int foreground = colourByte & NibbleBitMask;
                int background = (colourByte >> NibbleBitLength) & NibbleBitMask;
                char asciiByte = Codepage437.GetCharacter(data[i]);
                characterTuples[i / CharacterTupleByteLength] = new CharacterTuple(asciiByte, foreground, background);
            }

            _characters = characterTuples;
        }

        public override string Directory => FileDirectory;

        public override string Extension => FileExtension;

        public override FileType FileType => FileType.EndScreen;

        public override byte[] AsBytes()
        {
            return (byte[])_characters.Clone();
        }

        /// <summary>
        /// Print the end screen to the console.
        /// </summary>
        public void PrintScreen()
        {
            Encoding original = Console.OutputEncoding;
            Console.OutputEncoding = new UTF8Encoding(false);

            int column = 0;

            foreach (CharacterTuple tuple in _characters)
            {
                Console.BackgroundColor = (ConsoleColor)tuple.Background;
                Console.ForegroundColor = (ConsoleColor)tuple.Foreground;

                if (column == EndScreenLineLength)
                {
                    column = 0;
                    Console.WriteLine();
                }

                ++column;
                Console.Write(tuple.Character);
            }

            Console.OutputEncoding = original;
        }
    }
}
