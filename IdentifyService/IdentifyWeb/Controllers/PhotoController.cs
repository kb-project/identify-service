using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace IdentifyWeb.Controllers
{
    public class PhotoController : ApiController
    {
    }
}
using IdentifyWeb.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;

namespace IdentifyWeb.Controllers
{
    public class PhotoController : ApiController
    {
        string rootPath;

        public PhotoController()
        {
            rootPath = HostingEnvironment.MapPath("~/images/");
        }

        [Route("Files")]
        public List<FilesInfo> GetFiles()
        {
            List<FilesInfo> files = new List<FilesInfo>();

            foreach (var item in Directory.GetFiles(rootPath))
            {
                FileInfo f = new FileInfo(item);
                files.Add(new FilesInfo() { FileName = f.Name });
            }
            return files;

        }

        [Route("Bytes/{fileName}/{ext}")]
        public HttpResponseMessage Get(string fileName, string ext)
        {
            //S1: Construct File Path
            var filePath = Path.Combine(rootPath, fileName + "." + ext);
            if (!File.Exists(filePath)) //Not found then throw Exception
                throw new HttpResponseException(HttpStatusCode.NotFound);

            HttpResponseMessage Response = new HttpResponseMessage(HttpStatusCode.OK);

            //S2:Read File as Byte Array
            byte[] fileData = File.ReadAllBytes(filePath);

            if (fileData == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);
            //S3:Set Response contents and MediaTypeHeaderValue
            Response.Content = new ByteArrayContent(fileData);
            Response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            return Response;
        }
        // GET api/<controller>
        //public IEnumerable<string> Get()
        //{
        //    return new string[] { "value1", "value2" };
        //}

        //// GET api/<controller>/5
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/<controller>
        //public void Post([FromBody]string value)
        //{
        //}

        //// PUT api/<controller>/5
        //public void Put(int id, [FromBody]string value)
        //{
        //}

        //// DELETE api/<controller>/5
        //public void Delete(int id)
        //{
        //}
    }
}