using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DAT10.Utils
{
    public static class Enums
    {
        public static string GetDescription(this Enum value)
        {
            FieldInfo info = value.GetType().GetField(value.ToString());

            var attribute = info.GetCustomAttribute<DescriptionAttribute>();
            return attribute.Description;
        }
    }
}
