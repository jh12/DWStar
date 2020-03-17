using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DAT10.Modules.Refinement.ColumnName
{
    public class PatternMatchNamesConfiguration
    {
        public List<PatternMatch> Patterns = new List<PatternMatch>
        {
            // Sources:
            // - https://blogs.oracle.com/fadevrel/entry/common_regular_expression_patterns
            new PatternMatch("Email", new Regex("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,4}$")),
            new PatternMatch("Phone", new Regex("^(?:\\+|00)[0-9]{2,3}[ -]?[0-9]{8,10}$")),
            new PatternMatch("ZIP", new Regex("^\\d{4}$")),
            new PatternMatch("ZIP", new Regex("^\\d{5}\\b(?:[- ]{1}\\d{4})?$"))
        };

        public PatternMatchNamesConfiguration()
        {
            //Patterns.Clear();
        }
    }

    public class PatternMatch
    {
        public string PatternName;
        public Regex Pattern;

        public PatternMatch(string patternName, Regex pattern)
        {
            PatternName = patternName;
            Pattern = pattern;
        }
    }
}
