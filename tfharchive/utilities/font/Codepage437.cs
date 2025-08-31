namespace tfharchive.utilities.font
{
    internal static class Codepage437
    {
        /// <summary>
        /// The characters use in the codepage 437.
        /// </summary>
        private static readonly char[] cp437Chars =
        [
            '\0','☺','☻','♥','♦','♣','♠','•','◘','○','◙',
            '♂','♀','♪','♫','☼','►','◄','↕','‼','¶',
            '§','▬','↨','↑','↓','→','←','∟','↔','▲','▼',

            '\u00A0','!','"','#','$','%','&','\'','(',')',
            '*','+',',','-','.','/','0','1','2',
            '3','4','5','6','7','8','9',':',';','<',
            '=','>','?','@','A','B','C','D','E','F',
            'G','H','I','J','K','L','M','N','O','P',
            'Q','R','S','T','U','V','W','X','Y','Z',
            '[','\\',']','^','_','`','a','b','c','d',
            'e','f','g','h','i','j','k','l','m','n',
            'o','p','q','r','s','t','u','v','w','x',
            'y','z','{','|','}','~','⌂',

            'Ç','ü','é','â','ä','à','å','ç','ê','ë',
            'è','ï','î','ì','Ä','Å','É','æ','Æ','ô',
            'ö','ò','û','ù','ÿ','Ö','Ü','¢','£','¥',
            '₧','ƒ','á','í','ó','ú','ñ','Ñ','ª','º',
            '¿','⌐','¬','½','¼','¡','«','»',

            '░','▒','▓','│','┤','╡','╢','╖','╕','╣',
            '║','╗','╝','╜','╛','┐','└','┴','┬','├',
            '─','┼','╞','╟','╚','╔','╩','╦','╠','═',
            '╬','╧','╨','╤','╥','╙','╘','╒','╓','╫',
            '╪','┘','┌','█','▄','▌','▐','▀','α','ß',
            'Γ','π','Σ','σ','µ','τ','Φ','Θ','Ω','δ',
            '∞','φ','ε','∩','≡','±','≥','≤','⌠','⌡',
            '÷','≈','°','∙','·','√','ⁿ','²','■', '\u00A0'
        ];

        /// <summary>
        /// Get the UTF-16 equivalent of the given codepage 437 character code.
        /// </summary>
        /// <param name="code">The code of the codepage 437 character.</param>
        /// <returns>The UTF-16 character equivalent of the given codepage 437 character code.</returns>
        /// <exception cref="ArgumentException">Thrown if the code is not in the range 0-255.</exception>
        public static char GetCharacter(int code)
        {
            if (code < 0 || code >= cp437Chars.Length) throw new ArgumentException("Code is out of range: " + code);
            return cp437Chars[code];
        }
    }
}
