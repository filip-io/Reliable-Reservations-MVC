using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Reliable_Reservations_MVC.Models;
using System.Text;

namespace Reliable_Reservations_MVC.Controllers
{
    public class CustomerController : Controller
    {
        private readonly HttpClient _client;
        private readonly string? _baseUri;

        public CustomerController(HttpClient httpClient, IConfiguration configuration)
        {
            _client = httpClient;
            _baseUri = configuration["ApiSettings:BaseUri"];
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Registered customers";

            var response = await _client.GetAsync($"{_baseUri}api/Customer/all");

            var json = await response.Content.ReadAsStringAsync();

            var customerList = JsonConvert.DeserializeObject<List<Customer>>(json);

            return View(customerList);
        }


        public IActionResult Create()
        {
            ViewData["Title"] = "Register customer";

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Create(CustomerCreateViewModel customer)
        {
            var json = JsonConvert.SerializeObject(customer);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_baseUri}api/Customer/create", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var createdCustomer = JsonConvert.DeserializeObject<Customer>(jsonResponse);

                return RedirectToAction("Details", new { id = createdCustomer.CustomerId });
            }

            // In case of failure, return to the Create view with an error message
            ModelState.AddModelError("", "Error creating customer.");
            return View(customer);
        }


        public async Task<IActionResult> Details(int id)
        {
            var response = await _client.GetAsync($"{_baseUri}api/Customer/{id}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var customer = JsonConvert.DeserializeObject<Customer>(jsonResponse);

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
        public async Task<IActionResult> Edit(Customer customer)
        {
            var json = JsonConvert.SerializeObject(customer);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _client.PutAsync($"{_baseUri}api/Customer/{customer.CustomerId}", content);

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