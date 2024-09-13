using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Reliable_Reservations_MVC.Models.Menu;

namespace Reliable_Reservations_MVC.Controllers
{
    public class MenuController : Controller
    {

        private readonly HttpClient _client;
        private readonly string? _baseUri;

        public MenuController(HttpClient httpClient, IConfiguration configuration)
        {
            _client = httpClient;
            _baseUri = configuration["ApiSettings:BaseUri"];
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "MENU";

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

            ViewBag.Message = "Unable to retrieve menu items";
            return View();
        }
    }
}
