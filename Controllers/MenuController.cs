using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Reliable_Reservations_MVC.Models.Menu;
using System.Net.Http.Headers;
using System.Text;

namespace Reliable_Reservations_MVC.Controllers
{
    public class MenuController : Controller
    {
        private readonly ILogger<MenuController> _logger;
        private readonly HttpClient _client;
        private readonly string? _baseUri;

        public MenuController(ILogger<MenuController> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _client = httpClient;
            _baseUri = configuration["ApiSettings:BaseUri"];
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Menu";

            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.MenuItemCreatedMessage = TempData["SuccessMessage"].ToString();
                TempData.Clear();
            }

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

        [Authorize]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add menu item";

            ViewBag.Categories = Enum.GetValues(typeof(Category)).Cast<Category>().Select(c => new
            {
                Value = c,
                Text = c.ToString()
            }).ToList();

            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(MenuItemCreateViewModel menuItemCreateViewModel)
        {
            var token = HttpContext.Request.Cookies["jwtToken"];

            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Post/Redirect/Get (PRG) Pattern with TempData

            var json = JsonConvert.SerializeObject(menuItemCreateViewModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_baseUri}api/MenuItem/create", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var createdMenuItem = JsonConvert.DeserializeObject<MenuItemViewModel>(jsonResponse);

                TempData["SuccessMessage"] = $"Successfully created new menu item in <b>{createdMenuItem?.Category}</b> category with name <b>{createdMenuItem?.Name}</b> and ID: <b>{createdMenuItem?.MenuItemId}</b>";

                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Error creating menu item.");
            return View(menuItemCreateViewModel);
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var response = await _client.GetAsync($"{_baseUri}api/MenuItem/{id}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var menuItem = JsonConvert.DeserializeObject<MenuItemViewModel>(jsonResponse);

                return View(menuItem);
            }

            return NotFound();
        }

        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var token = HttpContext.Request.Cookies["jwtToken"];

            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var reponse = await _client.GetAsync($"{_baseUri}api/MenuItem/{id}");

            var json = await reponse.Content.ReadAsStringAsync();

            var menuItem = JsonConvert.DeserializeObject<MenuItemEditViewModel>(json);

            return View(menuItem);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(MenuItemEditViewModel menuItemEditViewModel)
        {
            var token = HttpContext.Request.Cookies["jwtToken"];

            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (!ModelState.IsValid)
            {
                return View(menuItemEditViewModel); // Return the model and validation error messages
            }
            var json = JsonConvert.SerializeObject(menuItemEditViewModel);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _client.PutAsync($"{_baseUri}api/MenuItem/{menuItemEditViewModel.MenuItemId}", content);

            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var token = HttpContext.Request.Cookies["jwtToken"];

            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _client.DeleteAsync($"{_baseUri}api/MenuItem/{id}");

            TempData["SuccessMessage"] = $"Successfully deleted menu item with ID: <b>{id}</b>";

            return RedirectToAction("Index");
        }
    }
}