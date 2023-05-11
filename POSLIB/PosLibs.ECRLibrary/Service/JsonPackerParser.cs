using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PosLibs.ECRLibrary.Service
{
    public  class JsonPackerParser
    {

        public JsonPackerParser() { }


        public static string CsvToJson(string csvData)
        {
          csvData = "posControllerId,dateTime,transactionID,transactionType,protocolType,requestBody,RFU1\n12,24042023233940,122333,7,1,4001,TX12345678,900,,,,,,,";

           
            List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
            using (StringReader reader = new StringReader(csvData))
            {
                
                string headerLine = reader.ReadLine();
                string[] headers = headerLine.Split(',');

                
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] values = line.Split(',');
                    Dictionary<string, string> row = new Dictionary<string, string>();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        row.Add(headers[i], values[i]);
                    }
                    data.Add(row);
                }
            }

          
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

           
            return json;
        }

       public static T JsonParser<T>(string jsonData)
        {
            T data = JsonConvert.DeserializeObject<T>(jsonData);
            return data;

        }
    }
}
