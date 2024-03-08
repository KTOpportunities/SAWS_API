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
    public class FeedbackController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private IWebHostEnvironment Environment;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IOptions<SmptSetting> _appSMTPSettings;
        private readonly ILogger<AuthenticateController> _logger;
        private readonly IUriService uriService;


        public FeedbackController(
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
        [Route("GetPagedAllFeedbacks")]
        [AllowAnonymous]
        
        public IActionResult GetPagedAllFeedbacks([FromQuery] PaginationFilter filter)
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

                var pagedData = _context.Feedbacks
                    .Where(d => d.isdeleted == false && d.broadcasterId == null)
                    .OrderByDescending(d => d.feedbackId)
               .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
               .Take(validFilter.PageSize)
               .ToList();

                var totalRecords = _context.Feedbacks.Where(d => d.isdeleted == false && d.broadcasterId == null).Count();

                var pagedReponse = PaginationHelper.CreatePagedReponse<Feedback>(pagedData, validFilter, totalRecords, uriService, route);
                return Ok(pagedReponse);

            }
            catch (Exception err)
            {
                string message = err.Message;
                return BadRequest(new Response { Status = "Error", Message = err.Message });
            }
        }

        [HttpGet]
        [Route("GetPagedAllFeedbacksByUniqueEmail")]
        [AllowAnonymous]        
        public IActionResult GetPagedAllFeedbacksByUniqueEmail([FromQuery] PaginationFilter filter)
        {
            DBLogic logic = new DBLogic(_context);

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            List<Feedback> toReturn;

            try
            {

                var allFeedbacks = _context.Feedbacks
                    .Where(d => d.isdeleted == false)
                    .OrderByDescending(d => d.feedbackId)
                    .ToList();


                var route = Request.Path.Value;
                var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);

                toReturn = allFeedbacks
                    .GroupBy(d => d.senderEmail)
                    .Select(group => group.First())
                    .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                    .Take(validFilter.PageSize)
                    .ToList();


                var pagedData = toReturn;

                var totalRecords = allFeedbacks
                                    .GroupBy(d => d.senderEmail)
                                    .Count();                

                var pagedReponse = PaginationHelper.CreatePagedReponse<Feedback>(pagedData, validFilter, totalRecords, uriService, route);
                return Ok(pagedReponse);

            }
            catch (Exception err)
            {
                string message = err.Message;
                return BadRequest(new Response { Status = "Error", Message = err.Message });
            }
        }

        [HttpGet]
        [Route("GetPagedAllBroadcasts")]
        [AllowAnonymous]        
        public IActionResult GetPagedAllBroadcasts([FromQuery] PaginationFilter filter)
        {
            DBLogic logic = new DBLogic(_context);

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            List<Feedback> toReturn;

            try
            {

                var allFeedbacks = _context.Feedbacks
                    .Where(d => d.isdeleted == false && d.broadcasterId != null)
                    .OrderByDescending(d => d.feedbackId)
                    .ToList();


                var route = Request.Path.Value;
                var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);

                toReturn = allFeedbacks
                    .GroupBy(d => d.batchId)
                    .Select(group => group.First())
                    .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
                    .Take(validFilter.PageSize)
                    .ToList();


                var pagedData = toReturn;

                var totalRecords = allFeedbacks
                                    .GroupBy(d => d.batchId)
                                    .Count();               

                var pagedReponse = PaginationHelper.CreatePagedReponse<Feedback>(pagedData, validFilter, totalRecords, uriService, route);
                return Ok(pagedReponse);

            }
            catch (Exception err)
            {
                string message = err.Message;
                return BadRequest(new Response { Status = "Error", Message = err.Message });
            }
        }

        [HttpPost("PostInsertNewFeedback")]
        [AllowAnonymous]
        public ActionResult<string> PostInsertNewFeedback( Feedback feedback)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            ObjectResult response = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });
            
            if (ModelState.IsValid)
            {
  
                DBLogic logic = new DBLogic(_context);
                    
                var DBResponse = logic.PostInsertNewFeeback(feedback);

                if (DBResponse == "Success")
                { 
                    response = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "Successfully added new feedback", DetailDescription = feedback }); 
                }
                else
                {
                    response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Failed to add new feeback" });
                }   
            }
            else
            {
                response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Model State Is Invalid" });
            }
            return response;
        }

        [HttpPost("PostInsertBroadcastMessages")]
        [AllowAnonymous]
        public ActionResult<string> PostInsertBroadcastMessages(List<Feedback> feedbackList)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

           ObjectResult response = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });

            DBLogic logic = new DBLogic(_context);

            var batchId = Guid.NewGuid().ToString();

            var broadcastId = Guid.NewGuid().ToString();

            foreach (Feedback feedback in feedbackList)
            {
                if (ModelState.IsValid)
                {
                          
                    var DBResponse = logic.PostInsertBroadcastMessages(feedback, batchId, broadcastId);

                    if (DBResponse == "Success")
                    {
                        response = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "Successfully added broadcast message", DetailDescription = feedback });
                    }
                    else
                    {
                        response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Failed to add broadcast message" });
                    }
                }
                else
                {
                    response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Model State Is Invalid" });
                }
            }

            return response;
        }


        [HttpDelete("DeleteFeedbackById")]
        public ActionResult<string> DeleteFeedbackById(int id)
        {

            ObjectResult response = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });

            if (ModelState.IsValid)
            {
                DBLogic logic = new DBLogic(_context);
                var DBResponse = logic.DeleteFeedback(id);
                if (DBResponse == "Success")
                { 
                    response = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "Successfully deleted Feedback" });
                }
                else
                {
                    response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Failed to delete feeback" });
                }
            }
            else
            {
                response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Model State Is Invalid" });
            }
            return response;
        }

        [HttpDelete("DeleteBroadcastByBatchId")]
        public ActionResult<string> DeleteBroadcastByBatchId(string batchId)
        {

            ObjectResult response = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });

            if (ModelState.IsValid)
            {
                DBLogic logic = new DBLogic(_context);
                var DBResponse = logic.DeleteBroadcast(batchId);
                if (DBResponse == "Success")
                { 
                    response = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "Successfully deleted Broadcast" });
                }
                else
                {
                    response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Failed to delete broadcast" });
                }
            }
            else
            {
                response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Model State Is Invalid" });
            }
            return response;
        }

        [HttpGet]
        [Route("GetFeedbackById")]
        public IActionResult GetFeedbackById(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            DBLogic logic = new DBLogic(_context);

            var feedbacks = logic.GetFeedbackById(id);

            return Ok(feedbacks);
        }

        [HttpGet]
        [Route("GetBroadcastMessages")]
        public IActionResult GetBroadcastMessages()
        {
            DBLogic logic = new DBLogic(_context);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            try
            {
                var broadcasts = logic.GetBroadcastMessages();
                return Ok(broadcasts);
            }
            catch (Exception err)
            {
                string message = err.Message;
                throw err;
            }
        }

        [HttpGet]
        [Route("GetFeedbackMessagesBySenderId")]
        public IActionResult GetFeedbackMessagesBySenderId(string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            DBLogic logic = new DBLogic(_context);

            var feedbacks = logic.GetFeedbackMessagesBySenderId(id);

            return Ok(feedbacks);
        }

    }
}
