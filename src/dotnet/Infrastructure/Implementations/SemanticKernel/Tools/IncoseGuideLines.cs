using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Implementations.SemanticKernel.Tools
{
    //todo, this should be perhaps sit in a database?

    public class IncoseGuideLinesTool
    {
        //private readonly IConfiguration _config;
        static Dictionary<string, List<GuideLines>> fileRecordsMap;

        public IncoseGuideLinesTool()
        {
            //_config = configuration;
            string filePath = "docs";
            if (fileRecordsMap == null)
            {
                fileRecordsMap = new Dictionary<string, List<GuideLines>>();
                LoadGuidelines(filePath);
            }
            
        }

        [KernelFunction("get_incose_rules")]
        [Description("Retrieve a list of compliancy rules regarding INCOSE")]
        public List<GuideLines> GetCompliancyRulesForCategory()
        {
            Console.WriteLine("--- Guideline Invoked ---");

            return fileRecordsMap["guidelines"];
            //var result = new StringBuilder();
            //foreach (var file in fileRecordsMap)
            //{
            //    var records = file.Value;
            //    foreach (var record in records)
            //    {
            //        result.AppendLine($"{record.Name}: {record.Definition} - { record.Instructions}");
            //    }
            //}
            //return result.ToString();
        }

        public void LoadGuidelines(string folderPath)
        {
            var csvFiles = Directory.GetFiles(folderPath, "guidelines.csv");

            foreach (var file in csvFiles)
            {
                string fileName = Path.GetFileName(file);
                fileName = fileName.Replace(".csv", "");
                List<GuideLines> recordsList = new();

                using (var reader = new StreamReader(file))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    HasHeaderRecord = true,
                    HeaderValidated = null, // Don't validate headers
                    MissingFieldFound = null // Don't throw on missing fields
                }))
                {
                    // Try to read the header row first to determine the actual column names
                    csv.Read();
                    csv.ReadHeader();
                    var headers = csv.HeaderRecord;

                    // Read records
                    var records = csv.GetRecords<GuideLines>();
                    foreach (var record in records)
                    {
                        recordsList.Add(record);
                    }
                }

                // Store the list of records with the filename as key
                fileRecordsMap.Add(fileName, recordsList);
            }

        }
    }


    public class GuideLines()
    { 
        public string? Name { get; set; }
        public string? Definition { get; set; }
        public string? Instructions { get; set; }
    }
}
