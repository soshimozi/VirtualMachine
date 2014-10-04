using System;
using System.Text;

namespace MacroAssembler
{
    public static class StringExtensions
    {
        public static string ReplaceAt(this string input, int index, char newChar)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            var builder = new StringBuilder(input);
            builder[index] = newChar;
            return builder.ToString();
        }

        /// Is a character 0-9 a-f A-F ?
        /// </summary>
        public static bool IsXDigit(this char c)
        {
            if ('0' <= c && c <= '9') return true;
            if ('a' <= c && c <= 'f') return true;
            if ('A' <= c && c <= 'F') return true;
            return false;
        }
    }
}
