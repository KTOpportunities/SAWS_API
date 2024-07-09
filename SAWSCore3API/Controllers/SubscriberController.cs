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
                recurringRequest.notify_url = request.NotifyUrl;
                recurringRequest.custom_int1 = request.userId;

                // recurringRequest.notify_url = this.payFastSettings.NotifyUrl;

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


        [HttpPost]
        [Route("Notify")]
        [AllowAnonymous]
        [MapToApiVersion("1")]
        public async Task<IActionResult> Notify([ModelBinder(BinderType = typeof(PayFastNotifyModelBinder))] PayFastNotify payFastNotifyViewModel)
        {

            payFastNotifyViewModel.SetPassPhrase(this.payFastSettings.PassPhrase);

            var calculatedSignature = payFastNotifyViewModel.GetCalculatedSignature();
            var isValidSignature = payFastNotifyViewModel.signature == calculatedSignature;
            this._logger.LogInformation($"Signature Validation Result: {isValidSignature}");

            // Validate IP Address
            var validIp = await ValidatePayFastIpAddress(this.HttpContext.Connection.RemoteIpAddress.ToString());
            this._logger.LogInformation($"IP Address Validation Result: {validIp}");

            // Validate Payment Data
            var cartTotal = 200.00; // Replace with actual cart total
            var validPaymentData = ValidatePaymentData(cartTotal, payFastNotifyViewModel.amount_gross);
            this._logger.LogInformation($"Payment Data Validation Result: {validPaymentData}");

            // Validate Server Confirmation
            // var payfastHost = this.payFastSettings.UseSandbox ? "sandbox.payfast.co.za" : "www.payfast.co.za";
            var payfastHost = "sandbox.payfast.co.za";
            var pfParamString = GetParamString(payFastNotifyViewModel);
            var validServerConfirmation = await ValidateServerConfirmation(payfastHost, pfParamString);
            this._logger.LogInformation($"Server Confirmation Validation Result: {validServerConfirmation}");

            // if (isValidSignature && validIp && validPaymentData && validServerConfirmation)
            // if (validIp)
            // {
            //     // All checks have passed, the payment is successful
            //     return Ok("Payment successful");
            // }
            // else
            // {
            //     // Some checks have failed, check payment manually and log for investigation
            //     return BadRequest("Payment validation failed");
            // }

            DBLogic logic = new DBLogic(_context);


            if (payFastNotifyViewModel.payment_status == "COMPLETE")
            {
                var subscription = logic.GetActiveSubscriptionByUserProfileId(int.Parse(payFastNotifyViewModel.custom_int1));

                if (subscription != null)
                {
                    subscription.subscription_token = payFastNotifyViewModel.token;

                    var DBResponse = logic.PostInsertSubcription(subscription);

                    var message = "";

                    if (DBResponse == "Success")
                    {
                        message = "Successfully updated subscription";
                    }
                    else
                    {
                        message = "Failed to add subscription";
                    }

                    return Ok(message);

                }
                else
                {
                    return Ok("Did not update subscription");
                }

            }

            // return Ok("Payment successful");
            return BadRequest("Payment validation failed");
        }

        private bool ValidatePaymentData(double cartTotal, string amount_gross)
        {
            // Validate that the cart total is approximately equal to the amount_gross received
            return Math.Abs(cartTotal - Convert.ToDouble(amount_gross)) <= 0.01;
        }

        private string GetParamString(PayFastNotify payFastNotifyViewModel)
        {
            var properties = payFastNotifyViewModel.GetType().GetProperties();
            var paramString = new StringBuilder();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(payFastNotifyViewModel)?.ToString();
                if (value != null && prop.Name != "signature")
                {
                    paramString.Append($"{prop.Name}={WebUtility.UrlEncode(value)}&");
                }
            }
            return paramString.ToString().TrimEnd('&');
        }

        private async Task<bool> ValidateServerConfirmation(string pfHost, string pfParamString)
        {
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync($"https://{pfHost}/eng/query/validate", new StringContent(pfParamString, Encoding.UTF8, "application/x-www-form-urlencoded"));
                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent == "VALID";
            }
        }

        private async Task<bool> ValidatePayFastIpAddress(string ipAddress)
        {
            var validHosts = new[] { "www.payfast.co.za", "sandbox.payfast.co.za", "w1w.payfast.co.za", "w2w.payfast.co.za" };
            var validIps = new HashSet<string>();

            foreach (var host in validHosts)
            {
                var ipAddresses = await Dns.GetHostAddressesAsync(host);
                foreach (var ip in ipAddresses)
                {
                    validIps.Add(ip.ToString());
                }
            }

            return validIps.Contains(ipAddress);
        }



        // [HttpPost]
        // [Route("Notify")]
        // [AllowAnonymous]
        // [MapToApiVersion("1")]
        // public async Task<IActionResult> Notify([ModelBinder(BinderType = typeof(PayFastNotifyModelBinder))]PayFastNotify payFastNotifyViewModel)
        // {
        //     payFastNotifyViewModel.SetPassPhrase(this.payFastSettings.PassPhrase);

        //     var calculatedSignature = payFastNotifyViewModel.GetCalculatedSignature();

        //     var isValid = payFastNotifyViewModel.signature == calculatedSignature;

        //     this._logger.LogInformation($"Signature Validation Result: {isValid}");

        //     // The PayFast Validator is still under developement
        //     // Its not recommended to rely on this for production use cases
        //     var payfastValidator = new PayFastValidator(this.payFastSettings, payFastNotifyViewModel, this.HttpContext.Connection.RemoteIpAddress);

        //     var merchantIdValidationResult = payfastValidator.ValidateMerchantId();

        //     this._logger.LogInformation($"Merchant Id Validation Result: {merchantIdValidationResult}");

        //     var ipAddressValidationResult = await payfastValidator.ValidateSourceIp();

        //     this._logger.LogInformation($"Ip Address Validation Result: {ipAddressValidationResult}");

        //     // Currently seems that the data validation only works for success
        //     if (payFastNotifyViewModel.payment_status == PayFastStatics.CompletePaymentConfirmation)
        //     {
        //         var dataValidationResult = await payfastValidator.ValidateData();

        //         this._logger.LogInformation($"Data Validation Result: {dataValidationResult}");
        //     }

        //     if (payFastNotifyViewModel.payment_status == PayFastStatics.CancelledPaymentConfirmation)
        //     {
        //         this._logger.LogInformation($"Subscription was cancelled");
        //     }

        //     return Ok();
        // }








        #endregion
    }
}
