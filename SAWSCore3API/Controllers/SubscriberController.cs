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
using Newtonsoft.Json.Linq;
using System.Net.Http;

using PayFast;
using PayFast.AspNetCore;
using SAWSCore3API.Wrappers;
using System.IO;
using System.Net;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Asn1.Crmf;


namespace SAWSCore3API.Controllers
{
    [ApiController]
    // [Route("api/[controller]")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    public class SubscriberController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly IOptions<SmptSetting> _appSMTPSettings;
        private readonly ILogger<AuthenticateController> _logger;
        private readonly IUriService uriService;

        private readonly PayFastSettings payFastSettings;




        public SubscriberController(UserManager<ApplicationUser> userManager,
                                      RoleManager<IdentityRole> roleManager,
                                      IConfiguration configuration,
                                      ApplicationDbContext dbContext,
                                      IOptions<SmptSetting> appSMTPSettings,
                                      ILogger<AuthenticateController> logger,
                                      IOptions<PayFastSettings> payFastSettings,
                                      IUriService uriService)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this._configuration = configuration;
            this._context = dbContext;
            this._appSMTPSettings = appSMTPSettings;
            _logger = logger;
            this.uriService = uriService;

            this.payFastSettings = payFastSettings.Value;

        }

        [HttpGet]
        [Route("GetPagedAllSubscribers")]
        [AllowAnonymous]
        [MapToApiVersion("1")]
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
                    .Where(d => d.isdeleted == false && d.userrole == "Subscriber")
                    .Include(d => d.Subscription)
                    .OrderByDescending(d => d.userprofileid)
               .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
               .Take(validFilter.PageSize)
               .ToList();
                var totalRecords = _context.userProfiles.Where(d => d.isdeleted == false && d.userrole == "Subscriber").Count();

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



        #region PayFast
        [HttpPost]
        [Route("MakeRecurringPayment")]
        [AllowAnonymous]
        [MapToApiVersion("1")]
        //PayFastRequest request
        public async Task<IActionResult> MakeRecurringPayment([FromBody] PaymentModel2 request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                var recurringRequest = new PayFastRequest(this.payFastSettings.PassPhrase);
                var subscritpitonUrl = new subscriptionresponse();

                // Merchant Details
                recurringRequest.merchant_id = _configuration.GetValue<string>("payFast:merchant_id");
                recurringRequest.merchant_key = _configuration.GetValue<string>("payFast:merchant_key");
                recurringRequest.return_url = request.returnUrl;
                recurringRequest.cancel_url = request.CancelUrl;
                recurringRequest.notify_url = this.payFastSettings.NotifyUrl;

                // Buyer Details
                recurringRequest.email_address = request.email_address;
                recurringRequest.name_first = request.name_first;
                recurringRequest.name_last = request.name_last;

                // Transaction Details
                recurringRequest.m_payment_id = request.m_payment_id;
                recurringRequest.amount = request.amount;
                recurringRequest.item_name = request.item_name;
                recurringRequest.item_description = request.item_description;

                // Transaction Options
                recurringRequest.email_confirmation = request.email_confirmation;
                recurringRequest.confirmation_address = request.confirmation_email;

                // Recurring Billing Details
                recurringRequest.subscription_type = SubscriptionType.Subscription;
                recurringRequest.billing_date = DateTime.Now;
                recurringRequest.recurring_amount = request.recurring_amount;

                if ((request.frequency).ToLower() == "monthly")
                {
                    recurringRequest.frequency = BillingFrequency.Monthly;
                }
                else
                {
                    recurringRequest.frequency = BillingFrequency.Annual;
                }

                // The number of payments/cycles that will occur for this subscription. Set to 0 for indefinite subscription.
                recurringRequest.cycles = 0;

                var redirectUrl = $"{this.payFastSettings.ProcessUrl}{recurringRequest.ToString()}";

                //int pos = redirectUrl.LastIndexOf("=") + 1;

                //var signature = redirectUrl.Substring(pos, redirectUrl.Length - pos);

                //Console.WriteLine(redirectUrl);

                //var redirectLink = _configuration.GetValue<string>("payFast:endPoint") + "/" + signature;
                var redirectLink = _configuration.GetValue<string>("payFast:endPoint") + "?" + redirectUrl;

                subscritpitonUrl.url = redirectLink.ToString();

                return Ok(subscritpitonUrl);

            }
            catch (Exception err)
            {
                string message = err.Message;
                //return BadRequest();
                //throw;
                return BadRequest(new Response { Status = "Error", Message = err.Message });
            }

        }

        [HttpPost]
        [Route("CancelSubscription")]
        [AllowAnonymous]
        [MapToApiVersion("1")]
        public async Task<IActionResult> CancelSubscription([FromBody] CancelSubscriptionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                DBLogic logic = new DBLogic(_context);

                using (var httpClient = new HttpClient())
                {
                    var endPoint = _configuration.GetValue<string>("payFast:endPoint");
                    var merchantId = _configuration.GetValue<string>("payFast:merchant_id");
                    var merchantKey = _configuration.GetValue<string>("payFast:merchant_key");
                    var DateTimeNow = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:sszzz");
                    var apiVersion = "v1";

                    // Set up the request URL and headers
                    // var requestUri = $"{endPoint}/subscriptions/{request.token}/cancel?testing={request.isTesting.ToString().ToLower()}";
                    httpClient.DefaultRequestHeaders.Add("merchant-id", merchantId);
                    httpClient.DefaultRequestHeaders.Add("version", apiVersion);
                    httpClient.DefaultRequestHeaders.Add("timestamp", DateTimeNow);
                    httpClient.DefaultRequestHeaders.Add("merchant-key", merchantKey); // Ensure this is included

                    var testUrl = $"https://api.payfast.co.za/subscriptions/{request.token}/cancel?testing={request.isTesting.ToString().ToLower()}";

                    var data = new Dictionary<string, string>
                    {
                        { "merchant-id", merchantId },
                        { "version", apiVersion },
                        { "timestamp", DateTimeNow },
                        { "merchant-key", merchantKey },
                        { "token", request.token }
                    };

                    var signature = logic.GenerateSignature(data);

                    httpClient.DefaultRequestHeaders.Add("signature", signature);

                    // Send the PUT request
                    var response = await httpClient.PutAsync(testUrl, null);

                    if (response.IsSuccessStatusCode)
                    {
                        var successResponse = await response.Content.ReadAsStringAsync();
                        return Ok(new
                        {
                            Response = successResponse,
                            Headers = httpClient.DefaultRequestHeaders.ToDictionary(h => h.Key, h => string.Join(", ", h.Value)),
                            signature = signature
                        });
                    }
                    else
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        return BadRequest(new Response { Status = "Error", Message = errorResponse });
                    }
                }
            }
            catch (Exception err)
            {
                return BadRequest(new Response { Status = "Error", Message = err.Message });
            }
        }








        #endregion
    }
}
