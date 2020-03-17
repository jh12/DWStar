using System.Collections.Generic;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;

namespace DAT10.Modules.Generation
{
    /// <summary>
    /// Provide a unique alias for tables
    /// </summary>
    public class AliasService
    {
        private static AliasService _instance;

        public static AliasService Instance
        {
            get
            {
                if(_instance == null)
                    _instance = new AliasService();

                return _instance;
            }
        }

        private Dictionary<Table, string> _tableAliases;
        private Dictionary<StarModelTableBase, string> _starTableAliases;
        private HashSet<string> _knownAliases;

        public AliasService()
        {
            _tableAliases = new Dictionary<Table, string>();    
            _starTableAliases = new Dictionary<StarModelTableBase, string>();    
            _knownAliases = new HashSet<string>();
        }

        public string GetAlias(Table table)
        {
            if (_tableAliases.ContainsKey(table))
                return _tableAliases[table];

            return AddAlias(table);
        }

        public string AddAlias(Table table)
        {
            if (_tableAliases.ContainsKey(table))
                return _tableAliases[table];

            int offset = 1;
            string alias;

            bool collision;

            string name = table.Name.Substring(0, 2);

            do
            {
                alias = "S_" + name + offset;

                collision = _knownAliases.Contains(alias);

                if (collision)
                    offset++;
            } while (collision);

            _knownAliases.Add(alias);
            _tableAliases.Add(table, alias);

            return alias;
        }

        public string GetAlias(StarModelTableBase table)
        {
            if (_starTableAliases.ContainsKey(table))
                return _starTableAliases[table];

            return AddAlias(table);
        }

        public string AddAlias(StarModelTableBase table)
        {
            if (_starTableAliases.ContainsKey(table))
                return _starTableAliases[table];

            int offset = 1;
            string alias;

            bool collision;

            string name = table.Name.Substring(0, 2);

            do
            {
                alias = "T_" + name + offset;

                collision = _knownAliases.Contains(alias);

                if (collision)
                    offset++;
            } while (collision);

            _knownAliases.Add(alias);
            _starTableAliases.Add(table, alias);

            return alias;
        }
    }
}
