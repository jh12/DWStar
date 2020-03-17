using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Utils.ArrayExtensions;

namespace DAT10.Utils
{
    public static class StringExtensions
    {
        public static string ToPascalCase(this string text)
        {
            if(text.Length < 2)
                return text.ToUpper();

            var strings = text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

            string result = "";
            foreach (var s in strings)
            {
                result += s.Substring(0,1).ToUpper() + s.Substring(1);
            }

            return result;
        }

        public static string ToCamelCase(this string text)
        {
            if (text.Length < 2)
                return text.ToUpper();

            var strings = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string result = "";
            result += strings[0].ToLower();

            for (int index = 1; index < strings.Length; index++)
            {
                var s = strings[index];
                result += s.Substring(0, 1).ToUpper() + s.Substring(1);
            }

            return result;
        }
    }
}
