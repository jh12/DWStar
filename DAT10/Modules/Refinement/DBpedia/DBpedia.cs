using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DAT10.Core;
using DAT10.Metadata.Model;
using DAT10.Modules.Inference;
using Newtonsoft.Json;
using SimpleLogger;

namespace DAT10.Modules.Refinement.DBpedia
{
    public class DBpedia : RefinementModuleBase
    {
        private readonly DataSampleService _sampleService;
        private DBpediaConfiguration _configuration;
        public override string Name { get; } = "Column Name Deduction";
        public override string Description { get; } = "Deduces name of a column, by looking up sampled data on DBpedia and find most commonly occurring term.";

        private string _cacheLocation;

        public DBpedia(DataSampleService sampleService, SettingsService settingsService) : base(CommonDependency.DataType, CommonDependency.Name)
        {
            _sampleService = sampleService;
            _configuration = settingsService.LoadSetting<DBpediaConfiguration>(this, "Settings").Result;

            _cacheLocation = Path.Combine(ModuleEngine.ModulePath(this), "Cache");
            if (!Directory.Exists(_cacheLocation))
                Directory.CreateDirectory(_cacheLocation);
        }

        public override async Task<CommonModel> Refine(CommonModel commonModel)
        {
            Uri uri = new Uri(_configuration.Endpoint);
            if (!uri.IsLoopback)
            {
                Logger.Log(Logger.Level.Warning, "Attempted to use non-local dbpedia instance. Skipping module execution.");
                return commonModel;
            }

            foreach (var table in commonModel.Tables)
            {
                foreach (var column in table.Columns)
                {
                    // Skip all non-string data types
                    if (!column.DataType.IsString())
                        continue;

                    // If column name confidence is above xx% then skip it
                    if (column.BestName.Confidence > _configuration.MinConfidence)
                        continue;

                    // Get value sample from column
                    var values = await _sampleService.GetDataSampleAsync(column, _configuration.SampleValues);

                    // Query DBpedia to get values
                    var classGuess = await GetMostLikelyClass(values);

                    if(classGuess.Label == null)
                        continue;

                    if (classGuess.Label.StartsWith("http://"))
                    {
                        Logger.Log($"[DBpedia]: The guessed classed return a URL, please check '{classGuess.Label}' and possibly add it to the synonyms list.");
                        continue;
                    }

                    if (classGuess.Confidence >= 0.10f)
                        column.AddNameCandidate(classGuess.Label.ToUpperInvariant(), classGuess.Confidence);
                }
            }

            return commonModel;
        }

        /// <summary>
        /// Query webclient with query value
        /// </summary>
        /// <param name="client">Active web client</param>
        /// <param name="query">Query string</param>
        /// <param name="usePersistentCache">When true, then the result is cached to disk and used for identical queries</param>
        /// <returns>A json string</returns>
        private async Task<string> QueryService(WebClient client, string query, bool usePersistentCache = true)
        {
            string cachePath = string.Empty;

            // Check if result exists in cache
            if (usePersistentCache)
            {
                cachePath = Path.Combine(_cacheLocation, $"QueryString.{query}.json");

                if (File.Exists(cachePath))
                {
                    // Return cached value
                    return File.ReadAllText(cachePath);
                }
            }

            // Clear client for next request
            client.Headers.Clear();
            client.QueryString.Clear();

            // Specify parameters
            client.QueryString.Add("QueryString", query);
            client.QueryString.Add("MaxHits", _configuration.MaxHits.ToString());

            // Request that the respons is given as json
            client.Headers.Add(HttpRequestHeader.Accept, "application/json");

            try
            {
                // Wait for result
                var result = await client.DownloadStringTaskAsync(_configuration.Endpoint);

                if (usePersistentCache)
                {
                    // Write result to cache
                    File.WriteAllText(cachePath, result);
                }

                return result;
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ConnectFailure)
                {
                    Logger.Log(Logger.Level.Error, $"Could not establish a connection to '{client.BaseAddress}'");
                }
                throw;
            }


        }

        private async Task<ClassGuess> GetMostLikelyClass(List<string> values)
        {
            List<DBpediaResults> results = new List<DBpediaResults>();

            // Create client for querying dbpedia
            using (WebClient client = new WebClient())
            {
                client.BaseAddress = _configuration.Endpoint;

                // Create a query for each value
                foreach (var value in values)
                {
                    if(string.IsNullOrWhiteSpace(value))
                        continue;

                    try
                    {
                        // Query DBpedia
                        var jsonString = await QueryService(client, value, _configuration.UseCache);

                        // Deserialize json string to predefined classes
                        var dbpediaresult = JsonConvert.DeserializeObject<DBpediaResults>(jsonString);
                        results.Add(dbpediaresult);
                    }
                    catch (WebException e)
                    {
                        if (e.Status == WebExceptionStatus.ConnectFailure)
                            throw e;

                        // If a 503 status code is returned, then wait before sending next query (to avoid hitting the ratelimit once again)
                        if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.ServiceUnavailable)
                        {
                            Logger.Log(Logger.Level.Info, "Possible ratelimit hit... Waiting 10 seconds before next query.");
                            await Task.Delay(TimeSpan.FromSeconds(10));
                        }
                        else
                        {
                            Logger.Log(e);
                        }

                    }
                }
            }

            // Compute a list of all found classes. Exclude specific labels from this list
            var classes = results.SelectMany(db => db.Results.SelectMany(r => r.Classes.Select(c => new { lbl = ReplaceLabel(c.Label), uri = c.URI })))
                .Where(c => !_configuration.IgnoreList.Contains(c.lbl)) // Ignore specific labels
                .ToList();

            // Group the classes by their label, and order them by their occurences
            var q = classes.GroupBy(c => c.lbl)
                           .Select(g => new { Label = g.Key, Count = g.Count() })
                           //.Where(o => !o.Label.StartsWith("http://")) // Ignore all url labels
                           .Where(o => o.Count > values.Count * (3f*(1f/4f))) // Only add classes which is found by 3/4 of the values
                           .OrderByDescending(x => x.Count).ToList();

            if(q.Count == 0)
                return new ClassGuess();

            var best = q.FirstOrDefault();

            float confidence = 1f;

            // If more than 25% of the results provided this label
            if (best.Count > (_configuration.MaxHits * values.Count) * 0.25f)
                confidence = 0.25f;

            // If more than 50% of the results provided this label
            if (best.Count > (_configuration.MaxHits * values.Count) * 0.50f)
                confidence = 0.50f;

            // If more than 75% of the results provided this label
            if (best.Count > (_configuration.MaxHits * values.Count) * 0.75f)
                confidence = 0.75f;

            // If 100% of the results provided this label
            if (best.Count > (_configuration.MaxHits * values.Count) * 1f)
                confidence = 0.80f;

            if (values.Count < 5)
                confidence = 0.05f;

            return new ClassGuess(best.Label, best.Count, confidence);

            //// Get the three best results
            //var top = q.Take(3).ToList();

            //// Only add if atleast three values is found
            //if(top.Count >= 3)
            //    return new ClassGuess(top[0].Label, top[0].Count, top[1].Count, top[2].Count);
            
            //return new ClassGuess();
        }

        private string ReplaceLabel(string label)
        {
            if (_configuration.Synonyms.ContainsKey(label))
                return _configuration.Synonyms[label];

            return label;
        }

        #region JSON classes

        [JsonObject]
        private class DBpediaResults
        {
            public List<Result> Results;
        }

        [JsonObject]
        private class Result
        {
            [JsonProperty("Label")]
            public string Label;
            [JsonProperty("Classes")]
            public List<ClassResult> Classes;
        }

        [JsonObject]
        private class ClassResult
        {
            [JsonProperty("Label")]
            public string Label;
            [JsonProperty("URI")]
            public string URI;
        }

        #endregion

        private struct ClassGuess
        {
            public string Label;
            public int Count;
            public float Confidence;

            public ClassGuess(string label, int count, float confidence)
            {
                Label = label.ToUpper();
                Count = count;
                Confidence = confidence;
            }
        }

    }
}
