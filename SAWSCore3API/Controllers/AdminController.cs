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
    [ApiController]
    // [Route("api/[controller]")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IOptions<SmptSetting> _appSMTPSettings;
        private readonly ILogger<AuthenticateController> _logger;
        private readonly IUriService uriService;


        public AdminController(UserManager<ApplicationUser> userManager,
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
        [Route("GetPagedAllAdmins")]
        [AllowAnonymous]
        [MapToApiVersion("1")]
        
        public IActionResult GetPagedAllAdmins([FromQuery] PaginationFilter filter)
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
                    .Where(d => d.isdeleted == false && d.userrole=="Admin")
                    .OrderByDescending(d => d.userprofileid)
               .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
               .Take(validFilter.PageSize)
               .ToList();
                var totalRecords = _context.userProfiles.Where(d => d.isdeleted == false && d.userrole=="Admin").Count();

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
    }
}
