using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Reliable_Reservations_MVC.Models.Customer;
using Reliable_Reservations_MVC.Models.Reservation;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Reliable_Reservations_MVC.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ILogger<CustomerController> _logger;
        private readonly HttpClient _client;
        private readonly string? _baseUri;

        public CustomerController(ILogger<CustomerController> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _client = httpClient;
            _baseUri = configuration["ApiSettings:BaseUri"];
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Registered customers";

            var token = HttpContext.Request.Cookies["jwtToken"];
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await _client.GetAsync($"{_baseUri}api/Customer/all");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var customerList = JsonConvert.DeserializeObject<List<CustomerViewModel>>(json);

                    if (customerList == null || !customerList.Any())
                    {
                        return View(new List<CustomerViewModel>());
                    }
                    return View(customerList);
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

            return View(new List<CustomerViewModel>());
        }

        [Authorize]
        public IActionResult Create()
        {
            ViewData["Title"] = "Register customer";

            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(CustomerCreateViewModel customerCreateViewModel)
        {
            var token = HttpContext.Request.Cookies["jwtToken"];

            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var json = JsonConvert.SerializeObject(customerCreateViewModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_baseUri}api/Customer/create", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var createdCustomer = JsonConvert.DeserializeObject<CustomerViewModel>(jsonResponse);

                TempData["SuccessMessage"] = $"Successfully created new customer {createdCustomer?.FirstName} {createdCustomer?.LastName} with ID: {createdCustomer?.CustomerId}";

                return RedirectToAction("Index");
            }

            var errorResponse = await response.Content.ReadAsStringAsync();
            var statusCode = response.StatusCode;

            if (statusCode == HttpStatusCode.Unauthorized)
            {
                ModelState.AddModelError("", "Unauthorized access. Please check your credentials.");
                return View(customerCreateViewModel);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Error creating reservation: {errorContent}");
                return View(customerCreateViewModel);
            }
        }



        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var token = HttpContext.Request.Cookies["jwtToken"];

            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _client.GetAsync($"{_baseUri}api/Customer/{id}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var customer = JsonConvert.DeserializeObject<CustomerViewModel>(jsonResponse);

                return View(customer);
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

            var response = await _client.GetAsync($"{_baseUri}api/Customer/{id}");

            var json = await response.Content.ReadAsStringAsync();

            var customer = JsonConvert.DeserializeObject<CustomerEditViewModel>(json);

            return View(customer);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(CustomerEditViewModel customerEditViewModel)
        {
            var token = HttpContext.Request.Cookies["jwtToken"];

            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (!ModelState.IsValid)
            {
                return View(customerEditViewModel);
            }

            var json = JsonConvert.SerializeObject(customerEditViewModel);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _client.PutAsync($"{_baseUri}api/Customer/{customerEditViewModel.CustomerId}", content);

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

            await _client.DeleteAsync($"{_baseUri}api/Customer/{id}");

            TempData["SuccessMessage"] = $"Successfully deleted customer with ID: <b>{id}</b>";

            return RedirectToAction("Index");
        }
    }
}