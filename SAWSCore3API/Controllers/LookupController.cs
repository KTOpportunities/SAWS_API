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
    public class LookupController : ControllerBase
    {
         private readonly UserManager<ApplicationUser> userManager;
        private IWebHostEnvironment Environment;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IOptions<SmptSetting> _appSMTPSettings;
        private readonly ILogger<AuthenticateController> _logger;
        private readonly IUriService uriService;


        public LookupController(
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

        [HttpGet("GetAllPackages")]
        [MapToApiVersion("1")]
        public IActionResult GetAllPackages()
        {
            DBLogic logic = new DBLogic(_context);

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                List<Package> records = new List<Package>();
                records = logic.GetAllPackages().ToList();

                return Ok(records);
            }
            catch (Exception err)
            {
                string message = err.Message;
                throw err;
            }
        }

        [HttpGet("GetServicesByPackageId")]
        [MapToApiVersion("1")]
        public IActionResult GetServicesByPackageId(int id)
        {
            DBLogic logic = new DBLogic(_context);

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                List<Service> records = new List<Service>();
                records = logic.GetServicesByPackageId(id).ToList();

                return Ok(records);
            }
            catch (Exception err)
            {
                string message = err.Message;
                throw err;
            }
        }

        [HttpGet("GetServiceProductsByServiceId")]
        [MapToApiVersion("1")]
        public IActionResult GetServiceProductsByServiceId(int id)
        {
            DBLogic logic = new DBLogic(_context);

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                List<ServiceProduct> records = new List<ServiceProduct>();
                records = logic.GetServiceProductsByServiceId(id).ToList();

                return Ok(records);
            }
            catch (Exception err)
            {
                string message = err.Message;
                throw err;
            }
        }


    }
}
