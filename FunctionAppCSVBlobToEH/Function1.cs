using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CSVBlobToEH
{
       public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task Run([BlobTrigger("csvblobs/{name}", Connection = "csvblobstore")]Stream myBlob, [EventHub("dest", Connection = "eventhubconnection")]IAsyncCollector<string> outputEvents, string name, ILogger log)
        {
            using (var reader = new StreamReader(myBlob))
            {
                //Read the first line of the CSV file and break into header values
                var line = reader.ReadLine();
                var headers = line.Split(',');

                //Read the rest of the file
                while (!reader.EndOfStream)
                {
                    //Create an empty string for our JSON
                    string outputJSON = "";
                    outputJSON = outputJSON + "{\n";
                    
                    //Read our lines one by one and split into values
                    line = reader.ReadLine();
                    var values = line.Split(',');

                    //Add all of the data except the last value
                    for (int i = 0; i < (values.Length - 1); i++)
                    {
                        outputJSON = outputJSON + "  \"" + headers[i] + "\": \"" + values[i] + "\",\n";
                    }
                    //Add the last value without a comma to properly form the JSON
                    int j = values.Length - 1;
                    outputJSON = outputJSON + "  \"" + headers[j] + "\": \"" + values[j] + "\"\n";
                    //Close the JSON
                    outputJSON = outputJSON + "}";
                    
                    await outputEvents.AddAsync(outputJSON);
                }
            }
        }
    }
}
