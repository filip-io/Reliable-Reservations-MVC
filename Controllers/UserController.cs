using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Reliable_Reservations_MVC.Models.User;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace Reliable_Reservations_MVC.Controllers
{
    public class UserController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserController> _logger;
        private readonly string? _baseUri;
        public UserController(HttpClient httpClient, ILogger<UserController> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUri = configuration["ApiSettings:BaseUri"];
            _logger = logger;
        }


        public async Task<IActionResult> Index()
        {
            return View();
        }


        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var response = await _httpClient.PostAsJsonAsync($"{_baseUri}api/User/login", loginViewModel);

                    if (!response.IsSuccessStatusCode)
                    {
                        ViewData["LoginFailed"] = "Invalid email or password.";
                        return View(loginViewModel);
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var token = JsonConvert.DeserializeObject<TokenResponse>(jsonResponse);

                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token.Token);

                    var claims = jwtToken.Claims.ToList();

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = jwtToken.ValidTo
                    });

                    HttpContext.Response.Cookies.Append("jwtToken", token.Token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = jwtToken.ValidTo
                    });

                    ViewData["ResponseError"] = "Unable to reach the API. Please try again later.";
                    return RedirectToAction("Index", "User");
                }

                return View(loginViewModel);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error reaching the API.");
                ViewData["ResponseError"] = "Unable to reach the API. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                ViewData["ResponseError"] = "An unexpected error occurred. Please try again later.";
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Response.Cookies.Delete("jwtToken");

            return RedirectToAction("Index", "Home");
        }
    }
}
