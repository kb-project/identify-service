using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

using Newtonsoft.Json;


namespace KBfunction
{
    public static class OcrQueueFunction
    {
        [FunctionName("QueueFunction")]
        public static void Run([QueueTrigger("ocr", Connection = "kbConnection")]string myQueueItem, TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");


            string jsontext = myQueueItem;
            Microsoft.ProjectOxford.Vision.Contract.OcrResults kbOCRresults = JsonConvert.DeserializeObject<Microsoft.ProjectOxford.Vision.Contract.OcrResults>(jsontext);
            foreach (var ocrRegion in kbOCRresults.Regions)
            {
                foreach (var line in ocrRegion.Lines)
                {
                    foreach (var word in line.Words)
                    {
                        log.Info(word.Text);
                    }
                }
            }


        }
    }
}
