using Microsoft.AspNetCore.Mvc;
using Reliable_Reservations_MVC.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Reliable_Reservations_MVC.Models.Menu;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HttpClient _client;
    private readonly string? _baseUri;

    public HomeController(ILogger<HomeController> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _client = httpClient;
        _baseUri = configuration["ApiSettings:BaseUri"];
    }

    public async Task<IActionResult> Index()
    {
        var popularMenuItems = await GetPopularMenuItemsAsync();
        return View(popularMenuItems);
    }

    private async Task<IEnumerable<MenuItemViewModel>> GetPopularMenuItemsAsync()
    {
        try
        {
            var response = await _client.GetAsync($"{_baseUri}api/MenuItem/all");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var menuItems = JsonConvert.DeserializeObject<IEnumerable<MenuItemViewModel>>(content);
                return menuItems?.Where(item => item.IsPopular) ?? Enumerable.Empty<MenuItemViewModel>();
            }

            _logger.LogError($"API call failed with status code: {response.StatusCode}");
            ViewData["ResponseError"] = "Error fetching popular menu items. Please try again later.";
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

        return Enumerable.Empty<MenuItemViewModel>();
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}