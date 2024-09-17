using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Reliable_Reservations_MVC.Models.Customer;
using Reliable_Reservations_MVC.Models.Reservation;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Reliable_Reservations_MVC.Controllers
{
    public class ReservationController : Controller
    {
        private readonly ILogger<ReservationController> _logger;
        private readonly HttpClient _client;
        private string? _baseUri;

        public ReservationController(ILogger<ReservationController> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _client = httpClient;
            _baseUri = configuration["ApiSettings:BaseUri"];
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Reservations";

            try
            {
                var response = await _client.GetAsync($"{_baseUri}api/Reservation/all");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    var reservationList = JsonConvert.DeserializeObject<List<ReservationDetailsViewModel>>(json);

                    if (reservationList == null || !reservationList.Any())
                    {
                        return View(new List<ReservationDetailsViewModel>());
                    }
                    return View(reservationList);
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

            return View(new List<ReservationDetailsViewModel>());
        }


        public IActionResult Create()
        {
            ViewData["Title"] = "Make reservation";

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Create(ReservationCreateViewModel reservationCreateViewModel)
        {
            // Post/Redirect/Get (PRG) Pattern with TempData

            var json = JsonConvert.SerializeObject(reservationCreateViewModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync($"{_baseUri}api/Reservation/create", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var createdReservation = JsonConvert.DeserializeObject<ReservationDetailsViewModel>(jsonResponse);

                TempData["SuccessMessage"] = 
                    $"Successfully created new reservation for " +
                    $"{createdReservation?.Customer?.FirstName} " +
                    $"{createdReservation?.Customer?.LastName} " +
                    $"with ID: {createdReservation?.ReservationId}";

                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Error creating customer.");
            return View(reservationCreateViewModel);
        }




















        public async Task<IActionResult> Details(int id)
        {
            var response = await _client.GetAsync($"{_baseUri}api/Reservation/{id}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var reservation = JsonConvert.DeserializeObject<ReservationDetailsViewModel>(jsonResponse);

                return View(reservation);
            }

            return NotFound();
        }
    }
}
