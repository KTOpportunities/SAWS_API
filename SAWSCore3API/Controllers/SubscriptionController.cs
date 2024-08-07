﻿using Microsoft.AspNetCore.Http;
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
    public class SubscriptionController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private IWebHostEnvironment Environment;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IOptions<SmptSetting> _appSMTPSettings;
        private readonly ILogger<AuthenticateController> _logger;
        private readonly IUriService uriService;


        public SubscriptionController(
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
        
        [HttpPost("PostInsertSubscription")]
        [AllowAnonymous]
        [MapToApiVersion("1")]
        public ActionResult<string> PostInsertSubscription( Subscription subscription)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            ObjectResult response = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });
            
            if (ModelState.IsValid)
            {
  
                DBLogic logic = new DBLogic(_context);
                    
                var DBResponse = logic.PostInsertSubcription(subscription);

                if (DBResponse == "Success")
                { 
                    response = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "Successfully added subscription", DetailDescription = subscription }); 
                }
                else
                {
                    response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Failed to add subscription" });
                }   
            }
            else
            {
                response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Model State Is Invalid" });
            }
            return response;
        }

        [HttpDelete("DeleteSubscriptionById")]
        [MapToApiVersion("1")]
        public ActionResult<string> DeleteSubscriptionById(int id)
        {

            ObjectResult response = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });

            if (ModelState.IsValid)
            {
                DBLogic logic = new DBLogic(_context);
                var DBResponse = logic.DeleteSubscription(id);
                if (DBResponse == "Success")
                { 
                    response = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "Successfully deleted subcription" });
                }
                else
                {
                    response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Failed to delete subcription" });
                }
            }
            else
            {
                response = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Failed", Message = "Model State Is Invalid" });
            }
            return response;
        }

        [HttpGet]
        [Route("GetSubscriptionById")]
        [MapToApiVersion("1")]
        public IActionResult GetSubscriptionById(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
            }

            DBLogic logic = new DBLogic(_context);

            var subscription = logic.GetSubscriptionById(id);

            return Ok(subscription);
        }

        [HttpGet("GetSubscriptionByUserProfileId")]
        [MapToApiVersion("1")]
        public IActionResult GetSubscriptionByUserProfileId(int Id)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                DBLogic logic = new DBLogic(_context);
                List<Subscription> records = logic.GetSubscriptionByUserProfileId(Id).ToList();

               /*
                if (records == null || !records.Any())
                {
                    return NotFound();
                }
               */

                return Ok(records);
            }
            catch (Exception err)
            {
                // throw err;
                return StatusCode(500, "Internal server error. Please try again later.");

            }
        }

        [HttpGet("GetActiveSubscriptionByUserProfileId")]
        [MapToApiVersion("1")]
        public IActionResult GetActiveSubscriptionByUserProfileId(int Id)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                DBLogic logic = new DBLogic(_context);
                List<Subscription> records = logic.GetActiveSubByUserProfileId(Id).ToList();

                var result = records.Select(s => new
                {
                    s.userprofileid,
                    s.package_name,
                    s.subscription_status
                });

                return Ok(result);
            }
            catch (Exception err)
            {
                // throw err;
                return StatusCode(500, "Internal server error. Please try again later.");

            }
        }

    }
}
