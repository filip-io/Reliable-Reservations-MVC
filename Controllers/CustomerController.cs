using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Reliable_Reservations_MVC.Models.Customer;
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

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Registered customers";

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


        public IActionResult Create()
        {
            ViewData["Title"] = "Register customer";

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Create(CustomerCreateViewModel customerCreateViewModel)
        {
            var json = JsonConvert.SerializeObject(customerCreateViewModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_baseUri}api/Customer/create", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var createdCustomer = JsonConvert.DeserializeObject<CustomerViewModel>(jsonResponse);

                return RedirectToAction("New", new { id = createdCustomer?.CustomerId });
            }

            ModelState.AddModelError("", "Error creating customer.");
            return View(customerCreateViewModel);
        }


        public async Task<IActionResult> New(int id)
        {
            var response = await _client.GetAsync($"{_baseUri}api/Customer/{id}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var customer = JsonConvert.DeserializeObject<CustomerViewModel>(jsonResponse);

                return View(customer);
            }

            return NotFound();
        }


        public async Task<IActionResult> Details(int id)
        {
            var response = await _client.GetAsync($"{_baseUri}api/Customer/{id}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var customer = JsonConvert.DeserializeObject<CustomerViewModel>(jsonResponse);

                return View(customer);
            }

            return NotFound();
        }


        public async Task<IActionResult> Edit(int id)
        {
            var response = await _client.GetAsync($"{_baseUri}api/Customer/{id}");

            var json = await response.Content.ReadAsStringAsync();

            var customer = JsonConvert.DeserializeObject<CustomerEditViewModel>(json);

            return View(customer);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(CustomerEditViewModel customerEditViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(customerEditViewModel); // Return the model and validation error messages
            }
            var json = JsonConvert.SerializeObject(customerEditViewModel);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _client.PutAsync($"{_baseUri}api/Customer/{customerEditViewModel.CustomerId}", content);

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _client.DeleteAsync($"{_baseUri}api/Customer/{id}");

            return RedirectToAction("Index");
        }
    }
}