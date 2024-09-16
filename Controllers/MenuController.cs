using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Reliable_Reservations_MVC.Models.Menu;

namespace Reliable_Reservations_MVC.Controllers
{
    public class MenuController : Controller
    {
        private readonly ILogger<MenuController> _logger;
        private readonly HttpClient _client;
        private readonly string? _baseUri;

        public MenuController(ILogger<MenuController>  logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _client = httpClient;
            _baseUri = configuration["ApiSettings:BaseUri"];
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Menu";

            try
            {
                var response = await _client.GetAsync($"{_baseUri}api/MenuItem/all");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var menuList = JsonConvert.DeserializeObject<List<MenuItemViewModel>>(json);

                    if (menuList == null || !menuList.Any())
                    {
                        return View(new List<MenuItemViewModel>());
                    }
                    return View(menuList);
                }
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

            ViewBag.Message = "Unable to retrieve menu items";
            return View();
        }
    }
}
