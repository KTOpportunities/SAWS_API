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
using System.Text;
using SAWSCore3API.Models;
using System.Threading;
using FluentFTP;
using FluentFTP.Rules;


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

        private const int LASTHOURS = 48;

        public object Extentions { get; private set; }

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
        public async Task<IActionResult> GetSourceTextFolderFiles(string textfoldername, int lasthours = LASTHOURS)
        {
            //get credentials from _config
            string ftpHost, username, password,rootFolder = "";
            ftpHost  = _configuration["FtpSettings:host"];
            username = _configuration["FtpSettings:username"];
            password = _configuration["FtpSettings:password"];
            rootFolder =  _configuration["FtpSettings:mainfolder"];
            string alerts = rootFolder + "/text/"+ textfoldername;//"/aviation/text/alerts";
            List<TextFile> textFiles = new List<TextFile>();
            List<FtpListItem> ftpListItems = new List<FtpListItem>();

            var client = new AsyncFtpClient(ftpHost, username, password); //new FtpClient(ftpHost, username, password);
            var asyncClient = new AsyncFtpClient(ftpHost, username, password);

            // connect to the server and automatically detect working FTP settings
            await client.AutoConnect();

            //var ftpListItems2 = await client.GetListing(alerts);
            // get a list of files and directories in the "/htdocs" folder
            DateTime fileAfterThisDateTime = DateTime.Now.AddHours(lasthours * -1);
            foreach (FtpListItem item in await client.GetListing(alerts))
            {

                // if this is a file
                if (item.Type == FtpObjectType.File)
                {

                    // get the file size
                    //long size = await client.GetFileSize(item.FullName);
                    string filename = item.Name;
                    DateTime fileModDateTime = await client.GetModifiedTime(item.FullName);
                    

                    if (fileModDateTime > fileAfterThisDateTime)
                    {
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

            }
            //order by lastmodified descending.
            textFiles = textFiles.OrderByDescending(d => d.lastmodified).ToList();
            return Ok(textFiles);

        }


        [HttpGet("GetSourceChartFolderFilesList")]
        public async Task<IActionResult> GetSourceChartFolderFilesList(string imagefoldername, int lasthours = LASTHOURS)
        {
            //get credentials from _config
            string ftpHost, username, password, rootFolder = "";
            ftpHost = _configuration["FtpSettings:host"];
            username = _configuration["FtpSettings:username"];
            password = _configuration["FtpSettings:password"];
            rootFolder = _configuration["FtpSettings:mainfolder"];
            string alerts = rootFolder + "/charts/" + imagefoldername;
            List<TextFile> textFiles = new List<TextFile>();
            List<FtpListItem> ftpListItems = new List<FtpListItem>();

            var client = new AsyncFtpClient(ftpHost, username, password); //new FtpClient(ftpHost, username, password);
            var asyncClient = new AsyncFtpClient(ftpHost, username, password);

            // connect to the server and automatically detect working FTP settings
            await client.AutoConnect();

            //var ftpListItems2 = await client.GetListing(alerts);
            // get a list of files and directories in the "/htdocs" folder
            DateTime fileAfterThisDateTime = DateTime.Now.AddHours(lasthours * -1);

            foreach (FtpListItem item in await client.GetListing(alerts))
            {

                // if this is a file
                if (item.Type == FtpObjectType.File)
                {

                    // get the file size
                    //long size = await client.GetFileSize(item.FullName);
                    string filename = item.Name;
                    DateTime fileModDateTime = await client.GetModifiedTime(item.FullName);

                    //var stream = new MemoryStream();

                    //if (!await client.DownloadStream(stream, item.FullName))
                    {
                        // throw new Exception("Cannot read file");
                        //continue;
                    }
                    //stream.Position = 0;
                    //StreamReader reader = new StreamReader(stream);
                    // string textContents = reader.ReadToEnd();

                    //We probably need to push this the db, then API would read from DB

                    if (fileModDateTime > fileAfterThisDateTime)
                    { 
                        TextFile textFile = new TextFile();
                        textFile.filename = filename;
                        textFile.foldername = imagefoldername;
                        textFile.lastmodified = fileModDateTime;
                        textFile.filetextcontent = "IMAGES CONTENT NOT LOADED";

                        textFiles.Add(textFile);
                    }
                }

            }
            //order by lastmodified descending.
            textFiles = textFiles.OrderByDescending(d => d.lastmodified).ToList();
            return Ok(textFiles);

        }


        [HttpGet("GetChartsFile")]
        public async Task<IActionResult> GetChartsFile(string imagefoldername, string imagefilename)
        {
            //get credentials from _config
            string ftpHost, username, password, rootFolder = "";
            ftpHost = _configuration["FtpSettings:host"];
            username = _configuration["FtpSettings:username"];
            password = _configuration["FtpSettings:password"];
            rootFolder = _configuration["FtpSettings:mainfolder"];
            string alerts = rootFolder + "/charts/" + imagefoldername;
            List<TextFile> textFiles = new List<TextFile>();
            List<FtpListItem> ftpListItems = new List<FtpListItem>();

            var client = new AsyncFtpClient(ftpHost, username, password); //new FtpClient(ftpHost, username, password);
            var asyncClient = new AsyncFtpClient(ftpHost, username, password);

            // connect to the server and automatically detect working FTP settings
            await client.AutoConnect();

            string downftpUrl = alerts + "/" + imagefilename;

            if (await client.FileExists(downftpUrl))
            {
                try 
                {
                    var item = await client.GetObjectInfo(downftpUrl);

                    if (item !=null)
                    { 
                        string filename = ""; string textfoldername = ""; string textContents = "";
                        DateTime fileModDateTime = await client.GetModifiedTime(item.FullName);

                        TextFile textFile = new TextFile();
                        textFile.filename = item.Name;
                        textFile.foldername = imagefoldername;
                        textFile.lastmodified = fileModDateTime;


                        var stream = new MemoryStream();

                        if (!await client.DownloadStream(stream, item.FullName))
                        {
                            // throw new Exception("Cannot read file");
                            
                        }

                        //stream.Position = 0;

                        textContents = Convert.ToBase64String(stream.ToArray()); 

                        //StreamReader reader = new StreamReader(stream);
                        //var streamData = reader.ReadToEnd();

                        textFile.filetextcontent = textContents;

                        return Ok(textFile);
                    }

                }
                catch (Exception err) 
                { 

                }
                //var net = new System.Net.WebClient();
                //TODO
                /*
                byte[] fileBytes;// = System.IO.File.ReadAllBytes(item.file_url);
                

                var downloadedFile = await client.DownloadBytes(downftpUrl);
                var contentType = item.file_mimetype;
                var fileName = item.file_origname;

                return File(downloadedFile, contentType, fileName);
                */
                return Ok("file exist on the ftp");
            }
            else
            {
                //return BadRequest(new { message : "File not found" });
                return BadRequest();
            }


            

            return Ok();

        }




        [HttpGet("SyncFTPFolders")]
        public async Task<IActionResult> SyncFTPFolders()
        {
            //get credentials from _config
            string ftpHost, username, password, rootFolder, fptmirrorfolder = "";
            ftpHost = _configuration["FtpSettings:host"];
            username = _configuration["FtpSettings:username"];
            password = _configuration["FtpSettings:password"];
            rootFolder = _configuration["FtpSettings:mainfolder"];
            fptmirrorfolder = _configuration["FtpSettings:fptmirrorfolder"];
            //using (var ftp = new FtpClient(ftpHost, username, password)) 
            {
                //ftp.Connect();
                var client = new AsyncFtpClient(ftpHost, username, password); //new FtpClient(ftpHost, username, password);
                //var asyncClient = new AsyncFtpClient(ftpHost, username, password);

                // connect to the server and automatically detect working FTP settings
                await client.AutoConnect();
                var token = new CancellationToken();

                List<FtpResult> synchResult = new List<FtpResult>();

                // download a folder and all its files
                try
                {
                    //var synchResult = await  client.DownloadDirectory(fptmirrorfolder, rootFolder, FtpFolderSyncMode.Update);
                    synchResult = await client.DownloadDirectory(@"c:\AviationAppFTPMirror\", rootFolder, FtpFolderSyncMode.Update, token: token);
                    //return Ok(synchResult);
                    return Ok(synchResult);
                }
                catch (Exception err) {
                    string errMsg = "";
                    errMsg = err.Message;//
                    return Ok(synchResult);
                }
                // download a folder and all its files, and delete extra files on disk
                //ftp.DownloadDirectory(@"C:\website\dailybackup\", @"/public_html/", FtpFolderSyncMode.Mirror);

            }

            return Ok();

        }

        [HttpGet("DownloadAllAsync")]
        public async Task<IActionResult> DownloadAllAsync()
        {
            string ftpHost, username, password, rootFolder, fptmirrorfolder = "";
            ftpHost = _configuration["FtpSettings:host"];
            username = _configuration["FtpSettings:username"];
            password = _configuration["FtpSettings:password"];
            rootFolder = _configuration["FtpSettings:mainfolder"];
            fptmirrorfolder = _configuration["FtpSettings:fptmirrorfolder"];

            var token = new CancellationToken();
            using (var ftp = new AsyncFtpClient(ftpHost, username, password))
            {
                await ftp.Connect(token);


                // download a folder and all its files
                await ftp.DownloadDirectory(@"c:\AviationAppFTPMirror\", rootFolder, FtpFolderSyncMode.Update, token: token);

                // download a folder and all its files, and delete extra files on disk
                //await ftp.DownloadDirectory(@"C:\website\dailybackup\", @"/public_html/", FtpFolderSyncMode.Mirror, token: token);

            }

            return Ok();
        }

        [HttpGet("DownloadAll")]
        public ActionResult DownloadAll()
        {
            string ftpHost, username, password, rootFolder, fptmirrorfolder = "";
            ftpHost = _configuration["FtpSettings:host"];
            username = _configuration["FtpSettings:username"];
            password = _configuration["FtpSettings:password"];
            rootFolder = _configuration["FtpSettings:mainfolder"];
            fptmirrorfolder = _configuration["FtpSettings:fptmirrorfolder"];

            using (var ftp = new FtpClient(ftpHost, username, password))
            {

                //ftp.Config.DataConnectionType = FtpDataConnectionType.PASV;
                //ftp.FtpTrace.LogToConsole = true;

                ftp.Connect();
                var result = ftp.GetListing();

                // download a folder and all its files
                ftp.DownloadDirectory(@"c:\AviationAppFTPMirror\", rootFolder, FtpFolderSyncMode.Update);

                // download a folder and all its files, and delete extra files on disk
                //ftp.DownloadDirectory(@"C:\website\dailybackup\", @"/public_html/", FtpFolderSyncMode.Mirror);

            }

            return Ok();
        }

        public List<FileInfo> GetFiles(string path, bool recursive = false)
        {
            //string path2 = System.Web.HttpContext.Current.Request.MapPath("~\\dataset.csv");

            List<FileInfo> files = new List<FileInfo>();
            DirectoryInfo di = new DirectoryInfo(path);
            files.AddRange(di.GetFiles());
            if (recursive)
            {
                var directories = di.GetDirectories();
                if (directories.Length > 0)
                    foreach (var dir in directories)
                        files.AddRange(GetFiles(dir.FullName, true));
            }
            IOrderedEnumerable<FileInfo> orderedfiles = null;
            bool descend = true;
            orderedfiles = descend? files.OrderByDescending(item => item.CreationTime): files.OrderBy(item => item.CreationTime);
            files = orderedfiles.ToList();
            return files;
           
        }


        [HttpGet("GetLocalTextFolderFiles")]
        public async Task<IActionResult> GetLocalTextFolderFiles(string textfoldername)
        {
            //get credentials from _config
            string ftpHost, username, password, rootFolder = "";
            ftpHost = _configuration["FtpSettings:host"];
            username = _configuration["FtpSettings:username"];
            password = _configuration["FtpSettings:password"];
            rootFolder = _configuration["FtpSettings:mainfolder"];
            string alerts = rootFolder + "/text/" + textfoldername;//"/aviation/text/alerts";
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
