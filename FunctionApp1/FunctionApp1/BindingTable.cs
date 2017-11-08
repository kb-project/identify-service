using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace FunctionApp1
{
    public static class BindingTable
    {
        [FunctionName("BindingTableEunk")]
        public static void Run2([QueueTrigger("ocr-test", Connection = "BlobConnection")]string myQueueItem, [Table("test", Connection = "TableConnection")]ICollector<Ocr> tableBinding, TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");
            var jsontext = myQueueItem;
            string ocrResult = "";
            Microsoft.ProjectOxford.Vision.Contract.OcrResults o = JsonConvert.DeserializeObject<Microsoft.ProjectOxford.Vision.Contract.OcrResults>(jsontext);
            foreach (var a in o.Regions)
            {
                foreach (var line in a.Lines)
                {
                    foreach (var word in line.Words)
                    {
                        log.Info(word.Text);
                        ocrResult = ocrResult + word.Text + " ";
                    }
                }
            }

            Guid guid = Guid.NewGuid();
            
            tableBinding.Add(new Ocr()
            {
                PartitionKey = guid.ToString(),
                RowKey = "1",
                OcrResult = ocrResult
            });


            log.Info(ocrResult);
        }


        public class Ocr
        {
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public string OcrResult { get; set; }
        }
    }
}