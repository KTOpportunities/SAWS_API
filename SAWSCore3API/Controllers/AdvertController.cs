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
    public class AdvertController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private IWebHostEnvironment Environment;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IOptions<SmptSetting> _appSMTPSettings;
        private readonly ILogger<AuthenticateController> _logger;
        private readonly IUriService uriService;


        public AdvertController(
            IWebHostEnvironment _environment,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            IOptions<SmptSetting> appSMTPSettings,
            ILogger<AuthenticateController> logger,
            IUriService uriService
            )
        {
            Environment = _environment;
            this.userManager = userManager;
            this.roleManager = roleManager;
            this._configuration = configuration;
            this._context = dbContext;
            this._appSMTPSettings = appSMTPSettings;
            _logger = logger;
            this.uriService = uriService;

        }

        [HttpGet]
        [Route("GetPagedAllAdverts")]
        [AllowAnonymous]
        [MapToApiVersion("1")]
        
        public IActionResult GetPagedAllAdverts([FromQuery] PaginationFilter filter)
        {
            DBLogic logic = new DBLogic(_context);

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                var route = Request.Path.Value;
                var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);

                var pagedData = _context.Adverts
                    .Where(d => d.isdeleted == false)
                    .OrderByDescending(d => d.advertId)
               .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
               .Take(validFilter.PageSize)
               .ToList();

                var totalRecords = _context.Adverts.Where(d => d.isdeleted == false).Count();

                var pagedReponse = PaginationHelper.CreatePagedReponse<Advert>(pagedData, validFilter, totalRecords, uriService, route);
                return Ok(pagedReponse);
            }
            catch (Exception err)
            {
                string message = err.Message;
                return BadRequest(new Response { Status = "Error", Message = err.Message });
            }
        }

        [HttpPost("PostInsertNewAdvert")]
        [AllowAnonymous]
        [MapToApiVersion("1")]
        public ActionResult<string> PostInsertNewFeedback( Advert advert)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            ObjectResult response = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });
            
            if (ModelState.IsValid)
            {
  
                DBLogic logic = new DBLogic(_context);
                    
                var DBResponse = logic.PostInsertNewAdvert(advert);

                if (DBResponse == "Success")
                { 
                    response = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "Successfully added new feedback", DetailDescription = advert }); 
                }
                else
                {
                    response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Failed to add new advert" });
                }   
            }
            else
            {
                response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Model State Is Invalid" });
            }
            return response;
        }

        [HttpPost("PostInsertAdvertClick")]
        [AllowAnonymous]
        [MapToApiVersion("1")]
        public ActionResult<string> PostInsertAdvertClick( AdvertClick advertClick)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            ObjectResult response = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });
            
            if (ModelState.IsValid)
            {
  
                DBLogic logic = new DBLogic(_context);
                    
                var DBResponse = logic.PostInsertAdvertClick(advertClick);

                if (DBResponse == "Success")
                { 
                    response = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "Successfully added click", DetailDescription = advertClick });
                }
                else
                {
                    response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Failed to add click" });
                }   
            }
            else
            {
                response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Model State Is Invalid" });
            }
            return response;
        }

        [HttpGet("GetAllAdverts")]
        [MapToApiVersion("1")]
        public IActionResult GetAllAdverts()
        {
            DBLogic logic = new DBLogic(_context);

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                List<Advert> records = new List<Advert>();
                records = logic.GetAllAdverts().ToList();

                var toReturn = records.Select(ad => new { advertId = ad.advertId,
                advert_url = ad.advert_url,
                 file_url = "http://qa.j-cred.co.za/aviationappapi/Uploads/" + ad.advertId + "/" + ad.DocAdverts.FirstOrDefault()?.DocTypeName + "/" + ad.DocAdverts.FirstOrDefault()?.file_origname }).ToList();

                return Ok(toReturn);
            }
            catch (Exception err)
            {
                string message = err.Message;
                throw err;
            }
        }

        [HttpGet]
        [Route("GetAdvertByAdvertId")]
        [MapToApiVersion("1")]
        public IActionResult GetAdvertByAdvertId(int id)
        {

            ObjectResult response = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

             if (ModelState.IsValid)
            {
                var advert = _context.Adverts
                    .Where(d => d.advertId == id && d.isdeleted == false)
                    .Include(d => d.DocAdverts)
                    .FirstOrDefault();

                if (advert != null)
                { 
                    string fileUrl = "http://qa.j-cred.co.za/aviationappapi/Uploads/" + advert.advertId + "/" + advert.DocAdverts.FirstOrDefault()?.DocTypeName + "/" + advert.DocAdverts.FirstOrDefault()?.file_origname;
                    
                    response = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "Successfully returned advert", DetailDescription = new {
                    Advert = advert,
                    FileUrl = fileUrl
                } }); 
                }
                else
                {
                    response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Failed to return advert" });
                }
            }

            return Ok(response);
        }

        [HttpDelete("DeleteAdvertById")]
        [MapToApiVersion("1")]
        public ActionResult<string> DeleteAdvertById(int id)
        {

            ObjectResult response = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });

            if (ModelState.IsValid)
            {
                DBLogic logic = new DBLogic(_context);
                var DBResponse = logic.DeleteAdvert(id);
                if (DBResponse == "Success")
                { 
                    response = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "Successfully deleted advert" });
                }
                else
                {
                    response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Failed to delete advert" });
                }
            }
            else
            {
                response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Model State Is Invalid" });
            }
            return response;
        }

    }
}
