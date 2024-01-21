using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using SAWSCore3API.Models;
using SAWSCore3API.DBModels;
using SAWSCore3API.Authentication;
using SAWSCore3API.Services;
using SAWSCore3API.Logic;

namespace SAWSCore3API.Controllers
{
    [Route("api/[controller]")]
    [EnableCors("DefaultCorsPolicy")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IOptions<SmptSetting> _appSMTPSettings;



        public AuthenticateController(
                   UserManager<ApplicationUser> userManager,
                   SignInManager<ApplicationUser> signInManager,
                   RoleManager<IdentityRole> roleManager,
                   ApplicationDbContext dbContext,
                   IConfiguration configuration,
                   IOptions<SmptSetting> appSMTPSettings
            )
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
            this._context = dbContext;
            this._appSMTPSettings = appSMTPSettings;
            _configuration = configuration;

        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginModel appUser)
        {
            ApplicationUser user = null;

            user = _context.User.Where(user => user.UserName == appUser.Username).SingleOrDefault();

            ObjectResult statusCode = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });

            if (user != null)
            {
                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                var authClaims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    };

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                    );

                //User toReturn = new User();

                //toReturn.Id = user.Id;
                //toReturn.EmailConfirmed = user.EmailConfirmed;
                //toReturn.Email = user.Email;
                //toReturn.UserName = user.UserName;
                //toReturn.PhoneNumber = user.PhoneNumber;
                //toReturn.PasswordHash = user.PasswordHash;
                //toReturn.Fullname = user.Fullname;
                //toReturn.IDNumber = user.IDNumber;
                //toReturn.token = new JwtSecurityTokenHandler().WriteToken(token);
                //toReturn.Gender = user.Gender;
                //sign in  
                var signInResult = await signInManager.PasswordSignInAsync(user, appUser.Password, false, false);
                //userObj = _context.users.Where(d => d.id == usermap.userid).SingleOrDefault();
                if (signInResult.Succeeded)
                {

                    statusCode = StatusCode(StatusCodes.Status200OK, new Response { Status = "200", Message = "Successfully Signed In", DetailDescription = signInResult });
                }
                else
                {
                    statusCode = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "401", Message = "Please check your password and username" });
                }
            }
            else if(user == null) {

                statusCode = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "401", Message = "Please check your password and username" });
            }
            else
            {
                statusCode = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "400", Message = "Bad request was made" });
            }
            return statusCode;

        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegisterModel appUser)
        {
            ObjectResult statusCode = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });

            //register functionality  
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = appUser.Username,
                    Email = appUser.Email,
                    
                };

                var result = await userManager.CreateAsync(user, appUser.Password);

                if (!await roleManager.RoleExistsAsync("Admin"))
                    await roleManager.CreateAsync(new IdentityRole("Admin"));

                if (!await roleManager.RoleExistsAsync("Subscriber"))
                    await roleManager.CreateAsync(new IdentityRole("Subscriber"));

                if (result.Succeeded)
                {
                    
                    await userManager.AddToRoleAsync(user, appUser.UserRole);

                    statusCode = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "User created successfully", Detail = result });
                }

                if (!result.Succeeded)
                {
                    statusCode = StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "User creation unsuccessful:", Detail = result });
                }
            }
            if (!ModelState.IsValid)
            {
                statusCode = StatusCode(StatusCodes.Status422UnprocessableEntity, new Response { Status = "Error", Message = "Model state is invalid , please check model" });
            }
            return statusCode;
        }



        [AllowAnonymous]
        [HttpPost("RegisterSubscriber")]
        public async Task<IActionResult> RegisterSubscriber(RegisterSubscriberModel appUser)
        {
            ObjectResult statusCode = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "", Message = "" });

            //register functionality  
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = appUser.Username,
                    Email = appUser.Email,

                };

                var result = await userManager.CreateAsync(user, appUser.Password);

                if (!await roleManager.RoleExistsAsync("Admin"))
                    await roleManager.CreateAsync(new IdentityRole("Admin"));

                if (!await roleManager.RoleExistsAsync("Subscriber"))
                    await roleManager.CreateAsync(new IdentityRole("Subscriber"));

                if (result.Succeeded)
                {

                    await userManager.AddToRoleAsync(user, appUser.UserRole);

                    statusCode = StatusCode(StatusCodes.Status200OK, new Response { Status = "Success", Message = "User created successfully", Detail = result });


                    
                    //create 
                    try
                    {
                        UserProfile userProfile = new UserProfile();
                        userProfile.userprofileid = 0;
                        userProfile.fullname = appUser.Fullname;
                        userProfile.email = appUser.Email;
                        //userProfile.mobilenumber = appUser.mo
                        userProfile.aspuid = user.Id;
                        userProfile.userrole = appUser.UserRole;
                        DBLogic logic = new DBLogic(_context);
                        logic.InsertUpdateUserProfile(userProfile);
                    }
                    catch (Exception err)
                    { 
                      //error creating user profile, but login was successfull
                    }
                }

                if (!result.Succeeded)
                {
                    statusCode = StatusCode(StatusCodes.Status400BadRequest, new Response { Status = "Error", Message = "User creation unsuccessful:", Detail = result });
                    // return BadRequest(ModelState.SelectMany(x => x.Value.Errors.Select(y => y.ErrorMessage)).ToList());
                }
            }
            if (!ModelState.IsValid)
            {
                statusCode = StatusCode(StatusCodes.Status422UnprocessableEntity, new Response { Status = "Error", Message = "Model state is invalid , please check model" });
            }
            return statusCode;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> InsertUpdateUserProfile(UserProfile userProfile)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }


            try
            {
                DBLogic logic = new DBLogic(_context);
                logic.InsertUpdateUserProfile(userProfile);

                return Ok(userProfile);

            }
            catch (Exception err)
            { 
            
            }

            return BadRequest();

        }

        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DeleteUserProfileById(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                DBLogic logic = new DBLogic(_context);
                logic.DeleteUserProfileById(id);
                return Ok();
            }
            catch (Exception err)
            {
                string message = err.Message;
                throw err;
            }
        }


        [AllowAnonymous]
        [HttpPost("Logout")]
        public async Task<IActionResult> LogOut(string username, string password)
        {
            try
            {

                await AuthenticationHttpContextExtensions.SignOutAsync(HttpContext, CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Login");

            }
            catch (Exception e)
            {

                return Ok(false);
            }

        }

        [HttpGet("GetAllUserLogins")]
        public async Task<IActionResult> GetAllUserLogins()
        {


            //var users = userManager.Users.Where(d => d.deleted == false);
            

            return Unauthorized(new { response = "Invalid users" });
        }


        [HttpPost("RequestPasswordReset")]
        public async Task<IActionResult> RequestPasswordReset(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return NotFound();
            }

            var user = await userManager.FindByNameAsync(email);

            if (user == null)
            {
                return Unauthorized(new { response = "Invalid email" });
            }


            if (user != null)
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);

                //Generate email with the new token
                byte[] resetTokenGeneratedBytes = Encoding.UTF8.GetBytes(resetToken);
                var validResetToken = Uri.EscapeDataString(WebEncoders.Base64UrlEncode(resetTokenGeneratedBytes));

                //var appUrl = _configuration["AppURL"]; //"http://localhost:4200/#/reset-password";//get URL from config
                //var appUrl = _configuration["AppURL"];

                var appUrl = "http://localhost:4200/";
                string resetUrl = appUrl + @"#/reset-password?email=" + email + "&token=" + validResetToken;

                string resetEmailBody = $"<h1>South African Weather Service</h1>" + $"<p>to reset your password <a href='{resetUrl}'>Clicking here</a></p>";

                try
                {
                    EmailService emailService = new EmailService(_configuration);
                    emailService.SendPasswordResetEmail(user.Email, resetEmailBody);
                }
                catch (Exception err)
                {

                }

                return Ok(new
                {
                    resetToken = validResetToken,
                    resetUrl = resetUrl
                });
            }

            return Unauthorized(new { response = "Invalid email" });
        }


        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] IDResetPassword iDResetPassword)
        {
            var user = await userManager.FindByNameAsync(iDResetPassword.email);

            if (user == null)
            {
                return Unauthorized(new { response = "Invalid email" });
            }

            if (iDResetPassword.newPassword != iDResetPassword.confirmPassword)
            {
                //
                return BadRequest(new { response = "Invalid email" });
            }

            if (user != null)
            {

                //get number of times the password hash has been used.
                var decodedResetToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(iDResetPassword.token));

                var result = await userManager.ResetPasswordAsync(user, decodedResetToken, iDResetPassword.newPassword);

                if (result.Succeeded)
                {

                    return Ok(new { response = "Password reset successful." });
                }
                else
                {
                    var errros = result.Errors.Select(e => e.Description);
                    return Unauthorized(errros);
                    //return Ok("Password reset");
                }
            }

            return Unauthorized(new { response = "Invalid email" });
        }

        [HttpGet("LoginUserNameExist")]
        public async Task<IActionResult> LoginUserNameExist(string username)
        {
            var user = await userManager.FindByNameAsync(username);

            if (user != null)
            {
                return Ok(true);
            }
            else
            {
                return Ok(false);
            }
        }

    }
}
