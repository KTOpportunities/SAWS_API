using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
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
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IOptions<SmptSetting> _appSMTPSettings;
        private readonly ILogger<AuthenticateController> _logger;
        private readonly IUriService uriService;


        public FeedbackController(UserManager<ApplicationUser> userManager,
                                      RoleManager<IdentityRole> roleManager,
                                      IConfiguration configuration,
                                      ApplicationDbContext dbContext,
                                      IOptions<SmptSetting> appSMTPSettings,
                                      ILogger<AuthenticateController> logger,
                                      IUriService uriService)
        {
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
        
        public IActionResult GetPagedAllUsers([FromQuery] PaginationFilter filter)
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

                var pagedData = _context.userProfiles
                    .Where(d => d.isdeleted == false && d.userrole=="Subscriber")
                    .OrderByDescending(d => d.userprofileid)
               .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
               .Take(validFilter.PageSize)
               .ToList();
                var totalRecords = _context.userProfiles.Where(d => d.isdeleted == false && d.userrole=="Subscriber").Count();

                //return Ok(new PagedResponse<List<cbbuser>>(pagedData, validFilter.PageNumber, validFilter.PageSize));
                var pagedReponse = PaginationHelper.CreatePagedReponse<UserProfile>(pagedData, validFilter, totalRecords, uriService, route);
                return Ok(pagedReponse);

            }
            catch (Exception err)
            {
                string message = err.Message;
                //return BadRequest();
                //throw;
                return BadRequest(new Response { Status = "Error", Message = err.Message });
            }
        }

        [HttpGet]
        [Route("GetAllSubsciberFeedbacks")]
        public async Task<IActionResult> GetAllSubsciberFeedbacks(string Id)
        {

            if (string.IsNullOrEmpty(Id))
            {
                return NotFound();
            }

            try 
            {

                 var user = await userManager.FindByIdAsync(Id);

                if (user != null)
                {
                    return Ok(true);
                }
                else
                {
                    return NotFound(new { response = "Feedbacks not found" });
                }

            }
            catch (Exception err)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "An error occurred while retrieving messages sent by the subscriber" });
            }

           
        }

        // [AllowAnonymous]
        // [HttpPost("SendFeedback")]
        // public IActionResult SendFeedback(Feedback feedback)
        // {
        //     if(feedback.subscriberEmail) {
        //         return  StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "Can not send feedback withouth email" });
        //     }

        //     try
        //     {
        //         // Store the feedback message in the database
        //         // _context.Feedback.Add(feedback);
        //         // _context.SaveChanges();
                
        //         return StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "Feedback sent successfully" });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "An error occurred while sending feedback" });
        //     }
        // }

        [HttpPost("PostInsertNewFeedback")]
        [AllowAnonymous]
        public ActionResult<string> PostInsertNewFeedback(Feedback feedback)
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

        [HttpPost("DeleteFeedback")]
        public ActionResult<string> DeleteFeedback(int id)
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

        [AllowAnonymous]
        [HttpPost]
        [Route("PostInsertUpdateFeedbackMessage")]
        public ActionResult PostInsertUpdateFeedbackMessage([FromBody] List<FeedbackMessage> feedbackMessages)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            DBLogic logic = new DBLogic(_context);

            var toReturn = new List<FeedbackMessage>();
            var dbItem = new FeedbackMessage();

            foreach (var feedbackMessage in feedbackMessages)
            {

                try
                {

                    dbItem.Id = feedbackMessage.Id;
                    dbItem.subcriberId = feedbackMessage.subcriberId;
                    dbItem.adminId = feedbackMessage.adminId;
                    dbItem.message = feedbackMessage.message;
                    // dbItem.created_at = ;
                    // dbItem.updated_at = ;
                    // dbItem.isdeleted = ;
                    // dbItem.deleted_at = ;

                    logic.InsertUpdateFeedbackMessage(dbItem);
                }
                catch (Exception err)
                {
                        string message = err.Message;
                        return BadRequest(new Response { Status = "Error", Message = "Error=" + err.Message + " InnerException=" + err.InnerException.Message });
                }
            
            }
            
            return Ok(toReturn);
        }


        [HttpGet]
        [Route("GetFeedbackMessageById")]
        public IActionResult GetFeedbackMessageById(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            DBLogic logic = new DBLogic(_context);

            var feedbacks = logic.GetFeedbackMessageById(id);

            return Ok(feedbacks);
        }

    }
}
