using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAT10.Metadata.Model;
using DAT10.StarModelComponents;

namespace DAT10.Modules.Multidimensional
{
    public class FactTableManyToOne : IMultidimModuleFact
    {
        public string Name { get; } = "Usually many-to-one";
        public string Description { get; } = "Applies confidence on the likelihood of a table being a fact table based on how many many-to-one connections it has. More many-to-one connections provides higher confidence.";
        public List<StarModel> TranslateModel(CommonModel commonModel)
        {
            List<Table> tables = commonModel.Tables;
            List<StarModel> starModels = new List<StarModel>();
            foreach (Table table in tables)
            {
                int amountOfManyToOne = table.Relations.Where(x => x.LinkTable == table && x.Cardinality == Cardinality.ManyToOne).ToList().Count;
                float confidence = 0;

                if (amountOfManyToOne < 2)
                    confidence = 0;
                else if (amountOfManyToOne == 2)
                    confidence = 0.7f;
                else if (amountOfManyToOne > 2)
                    confidence = 0.9f;

                starModels.Add(new StarModel(new FactTable(table, confidence), commonModel));
            }
            return starModels;
        }
    }
}
