using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SAWSCore3API.DBModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using FluentFTP;
using System.Text;

namespace SAWSCore3API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class RawSourceController : ControllerBase
    {
        private IWebHostEnvironment environment;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public RawSourceController(
            IWebHostEnvironment _environment,
            ApplicationDbContext dbContext,
            IConfiguration configuration
            )
        {
            this.environment = _environment;
            this._context = dbContext;
            this._configuration = configuration;
        }

        [HttpGet("getSourceTestFolderFiles")]
        public async Task<IActionResult> getSourceTestFolderFiles(string folderName)
        {
            //get credentials from _config
            string ftpHost, username, password,rootFolder = "";
            ftpHost  = _configuration["FtpSettings:host"];
            username = _configuration["FtpSettings:username"];
            password = _configuration["FtpSettings:password"];
            rootFolder = "/" + _configuration["FtpSettings:AviationData"];
            string alerts = rootFolder + "/htdocs/text/alerts";
            List<string> textFiles = new List<string>();

            var client = new AsyncFtpClient(ftpHost, username, password); //new FtpClient(ftpHost, username, password);
            var asyncClient = new AsyncFtpClient(ftpHost, username, password);

            // connect to the server and automatically detect working FTP settings
            await client.AutoConnect();

            // get a list of files and directories in the "/htdocs" folder
            foreach (FtpListItem item in await client.GetListing( rootFolder))
            {

                // if this is a file
                if (item.Type == FtpObjectType.File)
                {

                    // get the file size
                    long size = await client.GetFileSize(item.FullName);

                    // calculate a hash for the file on the server side (default algorithm)
                    FtpHash hash = await client .GetChecksum(item.FullName);

                    if (await client.DirectoryExists(alerts))
                    {
                        // download a folder and all its files
                        //await client.DownloadDirectory(@"C:\website\logs\", @"/public_html/logs", FtpFolderSyncMode.Update);
                        var stream = new MemoryStream();

                        if (!await client.DownloadStream(stream, item.FullName))
                        {
                            throw new Exception("Cannot read file");
                        }
                        stream.Position = 0;
                        StreamReader reader = new StreamReader(stream);
                        string textContents = reader.ReadToEnd();
                        textFiles.Add(textContents);
                        //string contents = Encoding.UTF8.GetString(stream);
                    }
                }

                // get modified date/time of the file or folder
                DateTime time = await client.GetModifiedTime(item.FullName);

            }

            
            


            return Ok(textFiles);

        }
    }
}
