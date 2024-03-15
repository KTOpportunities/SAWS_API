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
using SAWSCore3API.Models;

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

        [HttpGet("GetSourceTextFolderFiles")]
        public async Task<IActionResult> GetSourceTextFolderFiles(string textfoldername)
        {
            //get credentials from _config
            string ftpHost, username, password,rootFolder = "";
            ftpHost  = _configuration["FtpSettings:host"];
            username = _configuration["FtpSettings:username"];
            password = _configuration["FtpSettings:password"];
            rootFolder = "/home/aviapp/" + _configuration["FtpSettings:mainfolder"];
            string alerts = rootFolder + "/text/"+ textfoldername;//"/aviation/text/alerts";
            List<TextFile> textFiles = new List<TextFile>();
            List<FtpListItem> ftpListItems = new List<FtpListItem>();

            var client = new AsyncFtpClient(ftpHost, username, password); //new FtpClient(ftpHost, username, password);
            var asyncClient = new AsyncFtpClient(ftpHost, username, password);

            // connect to the server and automatically detect working FTP settings
            await client.AutoConnect();
  
            //var ftpListItems2 = await client.GetListing(alerts);
            // get a list of files and directories in the "/htdocs" folder
            foreach (FtpListItem item in await client.GetListing(alerts))
            {

                // if this is a file
                if (item.Type == FtpObjectType.File)
                {

                    // get the file size
                    //long size = await client.GetFileSize(item.FullName);
                    string filename = item.Name;
                    DateTime fileModDateTime = await client.GetModifiedTime(item.FullName);
                  
                    var stream = new MemoryStream();

                    if (!await client.DownloadStream(stream, item.FullName))
                    {
                    // throw new Exception("Cannot read file");
                        continue;
                    }
                    stream.Position = 0;
                    StreamReader reader = new StreamReader(stream);
                    string textContents = reader.ReadToEnd();

                    //We probably need to push this the db, then API would read from DB
                    TextFile textFile = new TextFile();
                    textFile.filename = filename;
                    textFile.foldername = textfoldername;
                    textFile.lastmodified = fileModDateTime;
                    textFile.filetextcontent = textContents;

                    textFiles.Add(textFile);
                }

            }
            //order by lastmodified descending.
            textFiles = textFiles.OrderByDescending(d => d.lastmodified).ToList();
            return Ok(textFiles);

        }
    }
}
