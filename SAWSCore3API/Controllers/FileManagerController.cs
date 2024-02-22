using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SAWSCore3API.Filters;
using SAWSCore3API.Logic;
using SAWSCore3API.Authentication;
using SAWSCore3API.DBModels;
using SAWSCore3API.Models;
using SAWSCore3API.Services;
using SAWSCore3API.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace SAWSCore3API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileManagerController : ControllerBase
    {
        private IWebHostEnvironment Environment;
        private readonly ApplicationDbContext _context;

        public FileManagerController(
            IWebHostEnvironment _environment,
            ApplicationDbContext dbContext
            )
        {
            Environment = _environment;
            this._context = dbContext;
        }

        [HttpPost]
        [Route("PostDocsForAdvert")]
        public IActionResult PostDocsForAdvert([FromForm] IList<DocAdvert> files)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            var toReturn = new List<DocAdvert>();
            DBLogic logic = new DBLogic(_context);
    
            foreach (var file in files)
            {
                var dbItem = new DocAdvert();

                try
                {
                    var folderId = Convert.ToString(file.advertId);
                    var rootPath = Path.Combine(Environment.ContentRootPath, "Uploads"); ;
                    //Create the Directory.
                    string path = Path.Combine(rootPath, rootPath + "\\" + folderId + "\\Advert\\");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    //Fetch the File.
                    IFormFile postedFile = file.file;                    

                    //extract file detains
                    string fileName = postedFile.FileName;
                    string fileUrl = Path.Combine(path, fileName);
                    string fileExtension = Path.GetExtension(postedFile.FileName);
                    long filesize = postedFile.Length;
                    string mimeType = postedFile.ContentType;

                    //Save the File.
                    using (FileStream stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                    {
                        postedFile.CopyTo(stream);
                    }

                    //populate dbFileObject less the raw file
                    dbItem.Id = file.Id;
                    dbItem.advertId = file.advertId;
                    dbItem.DocTypeName = file.DocTypeName;
                    dbItem.file_origname = fileName;
                    dbItem.file_url = fileUrl;
                    dbItem.file_size = filesize;
                    dbItem.file_mimetype = mimeType;
                    dbItem.file_extention = fileExtension;
                    if (file.Id == 0)
                    {
                        dbItem.created_at = DateTime.Now;
                        dbItem.updated_at = DateTime.Now;
                        dbItem.isdeleted = false;
                    }
                    else
                    {
                        dbItem.updated_at = DateTime.Now;
                        dbItem.isdeleted = file.isdeleted;
                    }


                    logic.InsertUpdateDocAdvert(dbItem);


                    toReturn.Add(dbItem);
                }
                catch (Exception err)
                {
                    string err0rMsg = err.Message;
                }
            }

            //Send OK Response to Client.
            return Ok(toReturn);
        }


        
    }
}
