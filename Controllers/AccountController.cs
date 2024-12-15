using LinkshellManager.Data;
using LinkshellManager.Interfaces;
using LinkshellManager.Models;
using LinkshellManager.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;


namespace LinkshellManager.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        public AccountController(UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ApplicationDbContext context, ILogger<AccountController> logger, IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var response = new LoginViewModel();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            if (!ModelState.IsValid) return View(loginViewModel);

            var user = await _userManager.FindByEmailAsync(loginViewModel.EmailAddress);

            if (user != null)
            {
                // User is found, check password
                var passwordCheck = await _userManager.CheckPasswordAsync(user, loginViewModel.Password);
                if (passwordCheck)
                {
                    // Password correct, sign in
                    var result = await _signInManager.PasswordSignInAsync(user, loginViewModel.Password, false, false);
                    if (result.Succeeded)
                    {
                        // Log the time zone for debugging
                        _logger.LogInformation("Time zone received: {TimeZone}", loginViewModel.TimeZone);

                        // Save the time zone to the AppUser model
                        user.TimeZone = loginViewModel.TimeZone;
                        await _userManager.UpdateAsync(user);

                        var roles = await _userManager.GetRolesAsync(user);
                        if (roles.Contains(UserRoles.Admin))
                        {
                            return RedirectToAction("Index", "Admin", new { id = user.Id });
                        }
                        else
                        {
                            return RedirectToAction("Index", "Dashboard");
                        }
                    }
                }
                // Password is incorrect
                TempData["Error"] = "Wrong credentials. Please try again";
                return View(loginViewModel);
            }
            // User not found
            TempData["Error"] = "Wrong credentials. Please try again";
            return View(loginViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                CharacterName = user.CharacterName,
                ProfileImageData = user.ProfileImage
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.CharacterName = model.CharacterName;

            if (model.ProfileImage != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await model.ProfileImage.CopyToAsync(memoryStream);
                    user.ProfileImage = memoryStream.ToArray();
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View("Profile", model);
            }

            return RedirectToAction("Profile");
        }

        // GET: Register
        [HttpGet]
        public IActionResult Register()
        {
            var response = new RegisterViewModel();
            return View(response);
        }

        // POST: Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
        {
            Console.WriteLine("Test1");
            if (!ModelState.IsValid) return View(registerViewModel);
            Console.WriteLine("Test2");
            var user = await _userManager.FindByEmailAsync(registerViewModel.EmailAddress);
            if (user != null)
            {
                TempData["Error"] = "This email address is already in use";
                return View(registerViewModel);
            }

            var newUser = new AppUser()
            {
                Email = registerViewModel.EmailAddress,
                UserName = registerViewModel.EmailAddress,
                CharacterName = registerViewModel.CharacterName,
            };

            var newUserResponse = await _userManager.CreateAsync(newUser, registerViewModel.Password);

            if (newUserResponse.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, UserRoles.User);

                // Send confirmation email
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
                var encodedToken = WebUtility.UrlEncode(token); // URL-encode the token
                var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = newUser.Id, token = encodedToken }, protocol: HttpContext.Request.Scheme);

                // Create the email body with a hyperlink
                var emailBody = $"Please confirm your email by clicking this link: <a href='{confirmationLink}'>Confirm your email</a>";

                await _emailSender.SendEmailAsync(registerViewModel.EmailAddress, "Confirm your email", emailBody);

                return RedirectToAction("RegisterConfirmation");
            }

            // Handle errors if user creation fails
            foreach (var error in newUserResponse.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(registerViewModel);
        }

        // GET: Register Confirmation
        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        // GET: Confirm Email
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                // Log error: missing userId or token
                Console.WriteLine("ConfirmEmail: Missing userId or token.");
                return View("Error");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                // Log error: user not found
                Console.WriteLine($"ConfirmEmail: User with ID {userId} not found.");
                return RedirectToAction("Index", "Home");
            }

            // Decode the token if it was URL-encoded
            var decodedToken = WebUtility.UrlDecode(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (result.Succeeded)
            {
                return View(true); // Email confirmed successfully
            }

            // Log error: confirmation failed
            Console.WriteLine($"ConfirmEmail: Email confirmation failed for user ID {userId} with token {token}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            return View(false); // Email confirmation failed
        }

        // GET: Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        // GET: Forgot Password
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Forgot Password
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Inform the user that the account does not exist
                ModelState.AddModelError(string.Empty, "No account found with that email.");
                return View(model);
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                // Inform the user that the email is not confirmed
                ModelState.AddModelError(string.Empty, "Email not confirmed.");
                return View(model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action(nameof(ResetPassword), "Account", new { token, email = model.Email }, protocol: Request.Scheme);
            await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        // GET: Forgot Password Confirmation
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: Reset Password
        [HttpGet]
        public IActionResult ResetPassword(string token = null)
        {
            if (token == null)
            {
                return View("Error");
            }

            var model = new ResetPasswordViewModel { Token = token };
            return View(model);
        }

        // POST: Reset Password
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Instead of redirecting, add an error message to the ModelState
                ModelState.AddModelError(string.Empty, "No account found with that email address.");
                return View(model); // Return the view with the model to display the error
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                if (error.Code == "InvalidToken") // This is an example; the actual code may vary
                {
                    ModelState.AddModelError(string.Empty, "The provided information is incorrect.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model); // Make sure to return the model here as well for consistency
        }

        // GET: Reset Password Confirmation
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var userLinkshells = await _context.AppUserLinkshells
                .Where(ul => ul.AppUserId == user.Id)
                .Select(ul => ul.Linkshell)
                .ToListAsync();
                
            var selectedLinkshell = userLinkshells.FirstOrDefault(l => l.Id == user.PrimaryLinkshellId);

            var model = new SettingsViewModel
            {
                Linkshells = userLinkshells,
                SelectedLinkshellId = selectedLinkshell?.Id
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SetPrimaryLinkshell(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var selectedLinkshell = await _context.Linkshells
                .FirstOrDefaultAsync(l => l.Id == model.SelectedLinkshellId);

            if (selectedLinkshell != null)
            {
                user.PrimaryLinkshellId = selectedLinkshell.Id;
                user.PrimaryLinkshellName = selectedLinkshell.LinkshellName;
                await _userManager.UpdateAsync(user);
            }

            // Get the URL of the previous page from the Referer header
            var refererUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(refererUrl))
            {
                return Redirect(refererUrl);
            }

            // Fallback to a default action if the Referer header is not available
            return RedirectToAction("Settings");
        }
    }
}
