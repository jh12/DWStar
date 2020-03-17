using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DAT10.Modules.Refinement.DBpedia
{
    public class DBpediaConfiguration
    {
        // At what confidence should we skip trying to find the column name
        public float MinConfidence = 0.75f;
        // How many samples to use against DBpedia
        public int SampleValues = 10;
        // How many hits should DBpedia return
        public int MaxHits = 2;
        // Endpoint of DBpedia lookup
        public string Endpoint = "http://localhost:1112/api/search/KeywordSearch";
        // Cache query results to disk?
        public bool UseCache = false;
        // Class labels to ignore
        public List<string> IgnoreList = new List<string> {"location", "place", "populated place", "owl#Thing", "HTTP://WWW.ONTOLOGYDESIGNPATTERNS.ORG/ONT/DUL/ D U L.OWL# AGENT" };
        // Class labels to translate into other values
        public Dictionary<string, string> Synonyms = new Dictionary<string, string>
        {
            ["http://www.wikidata.org/entity/ q486972"] = "human settlement",
        };
    }
}
