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

        [HttpGet("GetRegistrationsPerUserType")]
        [MapToApiVersion("1")]
        public IActionResult GetRegistrationsPerUserType()
        {
            var startDate = DateTime.Now.AddMonths(-12);
            var monthNames = new[]
            {
                    "January", "February", "March", "April", "May", "June",
                    "July", "August", "September", "October", "November", "December"
            };

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                var records = _context.userProfiles
                .Where(u => u.created_at >= startDate && u.isdeleted == false)
                 .GroupJoin(
                    _context.Subscriptions,
                    userProfile => userProfile.userprofileid,
                    subscription => subscription.userprofileid,
                    (userProfile, subscriptions) => new { userProfile, subscriptions }
                )
                .SelectMany(
                    us => us.subscriptions.DefaultIfEmpty(),
                    (us, subscription) => new { 
                        us.userProfile, 
                        // subscription
                        SubscriptionType = us.userProfile.userrole == "Subscriber" && subscription != null 
                                  ? (subscription.package_name.Contains("Regulated") || subscription.package_name.Contains("Premium")  ? "Subscribed"
                                    // : subscription.package_name.Contains("Premium") ? "Premium" 
                                    : subscription.package_name.Contains("Free") ? "Free" 
                                    : null)
                                  : null
                        }
                )
                .GroupBy(u => new 
                    { 
                        u.userProfile.userrole, 
                        Month = u.userProfile.created_at.Value.Month, 
                        Year = u.userProfile.created_at.Value.Year, 
                        SubscriptionType = u.SubscriptionType ?? (u.userProfile.userrole == "Admin" ? null : "Free"),
                        // SubscriptionType = u.SubscriptionType, 
                    }
                )
                .Select(g => new
                {
                    g.Key.userrole,
                    SubscriptionType = g.Key.SubscriptionType ?? "",
                    g.Key.Month,
                    g.Key.Year,
                    Registrations = g.Count()
                })
                .ToList();

                var result2 = records.Select(r => new
                    {
                        UserRole = r.userrole,
                        Month = r.Month,
                        SubscriptionType= r.SubscriptionType,
                        MonthString = monthNames[r.Month - 1],
                        Year = r.Year,
                        Registrations = r.Registrations
                }).OrderBy(r => r.Year).ThenBy(r => r.Month).ToList();

                var result = records
                        .GroupBy(r => new { r.Year, r.Month })
                        .Select(g => new
                            {
                                MonthString = monthNames[g.Key.Month - 1],
                                Month = g.Key.Month,
                                Year = g.Key.Year,
                                UserTypes = g.Select(r => new
                                {
                                    r.userrole,
                                    r.SubscriptionType,
                                    r.Registrations
                                }).ToList()
                            })
                        .OrderBy(g => g.Year)
                        .ThenBy(g => g.Month)
                        .ToList();

                return Ok(result);
            }
            catch (Exception err)
            {
                string message = err.Message;
                throw err;
            }
        }

        [HttpGet("GetSubscriptionsPerPackageType")]
        [MapToApiVersion("1")]
        public IActionResult GetSubscriptionsPerPackageType()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                var userSubscriptionsCounts = _context.userProfiles
                    .Where( u => u.isdeleted == false)
                    .GroupJoin(
                        _context.Subscriptions,
                        userProfile => userProfile.userprofileid,
                        subscription => subscription.userprofileid,
                        (userProfile, subscriptions) => new { userProfile, subscriptions }
                    )
                    .SelectMany(
                        us => us.subscriptions.DefaultIfEmpty(),
                        (us, subscription) => new { 
                            us.userProfile,
                            SubscriptionType = us.userProfile.userrole == "Subscriber" && subscription != null 
                                    ? (subscription.package_name.Contains("Regulated")  ? "Regulated"
                                        : subscription.package_name.Contains("Premium") ? "Premium"
                                        : subscription.package_name.Contains("Free") && subscription.subscription_status == "Active" ? "Free" 
                                        : null)
                                    : null
                            }
                    )           
                    .GroupBy(u => new 
                        { 
                            u.userProfile.userrole,
                            SubscriptionType = u.SubscriptionType ?? (u.userProfile.userrole == "Admin" ? "Admin" : null)
                        }
                    )
                    .Select(g => new
                    {
                        UserRole = g.Key.userrole,
                        SubscriptionType = g.Key.SubscriptionType,
                        Subscriptions = g.Count()
                    })           
                    .ToList();

                var newUserSubscriptionsCounts = userSubscriptionsCounts.Where(g => !(g.UserRole == "Subscriber" && g.SubscriptionType == null));
                var totalCount = newUserSubscriptionsCounts.Sum(usc => usc.Subscriptions);

                var response = new
                {
                    UserSubscriptionCounts = newUserSubscriptionsCounts,
                    TotalCount = totalCount
                };

                return Ok(response);
            }
            catch (Exception err)
            {
                string message = err.Message;
                throw err;
            }
        }

        [HttpGet("GetAdvertsClickPerMonth")]
        [MapToApiVersion("1")]
        public IActionResult GetAdvertsClickPerMonth()
        {
            var startDate = DateTime.Now.AddMonths(-12);
            var monthNames = new[]
            {
                    "January", "February", "March", "April", "May", "June",
                    "July", "August", "September", "October", "November", "December"
            };

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                var records = _context.AdvertClicks
               .Where(u => u.created_at >= startDate && u.isdeleted == false)
               .GroupBy(u => new { Month = u.created_at.Value.Month, Year = u.created_at.Value.Year })
               .Select(g => new
               {
                   g.Key.Month,
                   g.Key.Year,
                   Clicks = g.Count()
               })
               .ToList();

                var result = records.Select(r => new
                {
                    Month = r.Month,
                    MonthString = monthNames[r.Month - 1],
                    Year = r.Year,
                    Clicks = r.Clicks
                }).ToList();

                return Ok(result);
            }
            catch (Exception err)
            {
                string message = err.Message;
                throw err;
            }
        }


    }
}
