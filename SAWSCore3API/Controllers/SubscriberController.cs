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
using System.Net.Http.Headers;


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
                                      IUriService uriService
                                      )
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
                // this.payFastSettings.PassPhrase = _configuration.GetValue<string>("payFast:passphrase");

                var recurringRequest = new PayFastRequest(this.payFastSettings.PassPhrase);
                var subscriptionUrl = new subscriptionresponse();

                // Merchant Details
                recurringRequest.merchant_id = _configuration.GetValue<string>("payFast:merchant_id");
                recurringRequest.merchant_key = _configuration.GetValue<string>("payFast:merchant_key");
                recurringRequest.return_url = request.returnUrl;
                recurringRequest.cancel_url = request.CancelUrl;
                recurringRequest.notify_url = request.NotifyUrl;
                recurringRequest.custom_int1 = request.userId;
                recurringRequest.custom_int2 = request.package_id;
                recurringRequest.custom_int3 = request.subscription_amount;
                recurringRequest.custom_str1 = request.package_name;
                recurringRequest.custom_str2 = request.subscription_type;

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

                subscriptionUrl.url = redirectLink.ToString();

                return Ok(subscriptionUrl);

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
        [Route("CancelSubscriptionByUserProfileId")]
        [MapToApiVersion("1")]
        public async Task<IActionResult> CancelSubscriptionByUserProfileId(int Id)
        {

            DBLogic logic = new DBLogic(_context);

            try
            {

                var activeSubscription = logic.GetActiveSubscriptionByUserProfileId(Id);
                var freeSubscription = logic.GetFreeSubscriptionByUserProfileId(Id);

                if (activeSubscription == null)
                {
                    return BadRequest(new Response { Status = "Error", Message = "No active subscription" });
                }

                // Do not cancel on payfast if it's a free subscription
                if (activeSubscription.subscriptionId == freeSubscription.subscriptionId)
                {
                    return Ok(new Response { Status = "Success", Message = "Free subscription active" });
                }

                activeSubscription.subscription_status = "Cancelled";
                freeSubscription.subscription_status = "Active";

                this.payFastSettings.MerchantId = _configuration.GetValue<string>("payFast:merchant_id");
                this.payFastSettings.MerchantKey = _configuration.GetValue<string>("payFast:merchant_key");
                this.payFastSettings.NotifyUrl = _configuration.GetValue<string>("payFast:NotifyUrl");
                this.payFastSettings.PassPhrase = _configuration.GetValue<string>("payFast:passphrase");
                bool istesting = _configuration.GetValue<bool>("payFast:isTesting");

                var subscriptionCancellation = new PayFastSubscription(this.payFastSettings);

                var result = await subscriptionCancellation.Cancel(activeSubscription.subscription_token, istesting);

                if (result.status == "success")
                {

                    var insertResponseCancel = logic.PostInsertSubcription(activeSubscription);
                    var insertResponseActivate = logic.PostInsertSubcription(freeSubscription);
                    var messageCancel = insertResponseCancel == "Success" ? "Successfully cancelled subscription" : "Failed to cancel subscription";
                    var messageActivate = insertResponseActivate == "Success" ? "Successfully activated free subscription" : "Failed to activate free subscription";

                    return Ok(new
                    {
                        Response = result,
                        CancelMessage = messageCancel,
                        ActiveMessage = messageActivate,
                        Status = "Success"
                    });
                }
                else
                {

                    return Ok(new
                    {
                        Response = result,
                        Status = "Failed"
                    });
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
            DBLogic logic = new DBLogic(_context);

            if (payFastNotifyViewModel.payment_status != "COMPLETE")
            {
                return BadRequest("Payment validation failed");
            }

            if (!int.TryParse(payFastNotifyViewModel.custom_int1, out int userId) ||
                !int.TryParse(payFastNotifyViewModel.custom_int2, out int packageId) ||
                !int.TryParse(payFastNotifyViewModel.custom_int3, out int packagePrice)
                )
            {
                return BadRequest("Invalid integer value in custom fields");
            }

            // Cancel existing subscription

            // var userId = int.Parse(payFastNotifyViewModel.custom_int1);
            var activeSubscription = logic.GetActiveSubscriptionByUserProfileId(int.Parse(payFastNotifyViewModel.custom_int1));

            if (activeSubscription == null)
            {
                return Ok("Did not find an active subscription");
            }

            activeSubscription.subscription_status = "Cancelled";

            this.payFastSettings.MerchantId = _configuration.GetValue<string>("payFast:merchant_id");
            this.payFastSettings.MerchantKey = _configuration.GetValue<string>("payFast:merchant_key");
            this.payFastSettings.NotifyUrl = _configuration.GetValue<string>("payFast:NotifyUrl");
            this.payFastSettings.PassPhrase = _configuration.GetValue<string>("payFast:passphrase");
            bool istesting = _configuration.GetValue<bool>("payFast:isTesting");

            var subscriptionCancellation = new PayFastSubscription(this.payFastSettings);

            // Cancel payfast subscription

            var result = await subscriptionCancellation.Cancel(activeSubscription.subscription_token, istesting);

            if (result.status != "success")
            {
                 return Ok("Did not cancel payfast subscription");
            }

            activeSubscription.updated_at = DateTime.Now;
            var cancelResponse = logic.PostInsertSubcription(activeSubscription);

            if (cancelResponse != "Success")
            {
                return Ok("Did not cancel subscription");
            }

            // Add new subscription 

            var newSubscription = new Subscription
            {
                subscriptionId = 0,
                userprofileid = userId,
                package_name = payFastNotifyViewModel.custom_str1,
                package_id = packageId,
                package_price = packagePrice,
                start_date = DateTime.Now,
                end_date = DateTime.Now.AddMonths(12),
                subscription_duration = 365,
                subscription_token = payFastNotifyViewModel.token,
                subscription_status = "Active"
            };

            var insertResponse = logic.PostInsertSubcription(newSubscription);
            var message = insertResponse == "Success" ? "Successfully updated subscription" : "Failed to add subscription";

            return Ok(message);
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
