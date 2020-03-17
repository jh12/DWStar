using System;
using System.Linq;
using DAT10.Core;
using DAT10.StarModelComponents;
using DAT10.StarModelComponents.Dimensions;
using DAT10.Utils;

namespace DAT10.Modules.StarRefinement.Naming
{
    public class NamingConvention : IStarRefinement
    {
        public string Name { get; } = "Naming Convention";
        public string Description { get; } = "Applies naming conventions to fact tables and dimension tables in order to fit the names to an organization’s standards.";

        private NamingConventionConfiguration _config;

        public NamingConvention(SettingsService settingsService)
        {
            _config = settingsService.LoadSetting<NamingConventionConfiguration>(this, "Settings").Result;
        }

        public StarModel Refine(StarModel starModel)
        {
            starModel.FactTable.RenameDuplicateColumns();
            FormatFactTable(starModel.FactTable);

            foreach (var dimension in starModel.Dimensions)
            {
                dimension.RenameDuplicateColumns();
                FormatDimension(dimension);
            }

            return starModel;
        }

        private void FormatFactTable(FactTable fact)
        {
            fact.Name = FormatName(fact.Name,
                _config.TableStripUnderscore,
                _config.TableNameCasing,
                _config.FactTableNameStructure);

            foreach (var column in fact.Columns)
            {
                column.Name = FormatName(column.Name,
                _config.ColumnStripUnderscore,
                _config.ColumnNameCasing,
                _config.ColumnNameStructure);
            }
        }

        private void FormatDimension(Dimension dim)
        {
            dim.Name = FormatName(dim.Name, 
                _config.TableStripUnderscore, 
                _config.TableNameCasing,
                _config.DimensionNameStructure);

            foreach (var column in dim.Columns)
            {
                column.Name = FormatName(column.Name,
                _config.ColumnStripUnderscore,
                _config.ColumnNameCasing,
                _config.ColumnNameStructure);
            }
        }

        public string FormatName(string name, bool stripUnderscore, string casing, string nameStructure)
        {
            if (stripUnderscore)
                name = name.Replace('_', ' ');

            name = ChangeCasing(name, casing);
            name = nameStructure.Replace("%NAME%", name);

            return name;
        }

        private string ChangeCasing(string value, string casing)
        {
            switch (casing.ToLower())
            {
                case "lowercase":
                    return value.ToLower();
                case "uppercase":
                    return value.ToUpper();
                case "pascalcase":
                    return value.ToPascalCase();
                case "camelcase":
                    return value.ToCamelCase();
                default:
                    return value;
            }
        }
    }
}
