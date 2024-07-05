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
    [ApiController]
    // [Route("api/[controller]")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
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
        [MapToApiVersion("1")]
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
                    // string path = Path.Combine(rootPath, rootPath + "\\" + folderId + "\\Advert\\");
                    string path = Path.Combine(rootPath, rootPath + "\\Advert\\" + folderId + "\\");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    //Fetch the File.
                    IFormFile postedFile = file.file;

                    //extract file detains
                    string fileName1 = postedFile.FileName;
                    string fileName = fileName1;
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

            return Ok(toReturn);
        }

        [HttpGet]
        [Route("GetDocAdvertFileById")]
        [MapToApiVersion("1")]
        public IActionResult GetDocAdvertFileById(int id)
        {

            ObjectResult response = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            DBLogic logic = new DBLogic(_context);

            var item = logic.GetDocAdvertFileById(id);

            if (item == null)
            {
                response = StatusCode(StatusCodes.Status404NotFound, new Response { Status = "Not Found", Message = $"Advert file with id: {id} to add new feedback" });

                return response;
            }

            var net = new System.Net.WebClient();
            byte[] fileBytes = System.IO.File.ReadAllBytes(item.file_url);
            var contentType = item.file_mimetype;
            var fileName = item.file_origname;

            return File(fileBytes, contentType, fileName);

        }

        [HttpDelete]
        [Route("DeleteDocAdvertById")]
        [MapToApiVersion("1")]
        public ActionResult<string> DeleteDocAdvertById(int Id)
        {

            ObjectResult response = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });

            if (ModelState.IsValid)
            {
                DBLogic logic = new DBLogic(_context);
                var DBResponse = logic.DeleteDocAdvert(Id);

                if (DBResponse == "Success")
                { 
                    response = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = $"Successfully deleted advert document with Id {Id}" }); 
                    }
                else
                {
                    response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = $"Failed to delete advert document with Id {Id}" });
                }
            }

            else
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            return response;
        }

        [HttpPost]
        [Route("PostDocsForFeedback")]
        [MapToApiVersion("1")]
        public IActionResult PostDocsForFeedback([FromForm] IList<DocFeedback> files)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            var toReturn = new List<DocFeedback>();
            DBLogic logic = new DBLogic(_context);
    
            foreach (var file in files)
            {
                var dbItem = new DocFeedback();

                try
                {
                    var folderId = Convert.ToString(file.feedbackMessageId);
                    var rootPath = Path.Combine(Environment.ContentRootPath, "Uploads"); ;
                    //Create the Directory.
                    // string path = Path.Combine(rootPath, rootPath + "\\" + folderId + "\\Feedback\\");
                    string path = Path.Combine(rootPath, rootPath + "\\Feedback\\" + folderId + "\\");
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
                    dbItem.feedbackMessageId = file.feedbackMessageId;
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


                    logic.InsertUpdateDocFeedback(dbItem);


                    toReturn.Add(dbItem);
                }
                catch (Exception err)
                {
                    string err0rMsg = err.Message;
                }
            }

            return Ok(toReturn);
        }




        
    }
}
