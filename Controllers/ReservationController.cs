using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Reliable_Reservations_MVC.Models;
using Reliable_Reservations_MVC.Models.Customer;
using Reliable_Reservations_MVC.Models.OpeningHours;
using Reliable_Reservations_MVC.Models.Reservation;
using Reliable_Reservations_MVC.Models.Table;
using Reliable_Reservations_MVC.Models.TimeSlot;
using System.Net.Http.Headers;
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

        [Authorize]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Reservations";

            var token = HttpContext.Request.Cookies["jwtToken"];

            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

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


        [Authorize]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Make reservation";
            var token = HttpContext.Request.Cookies["jwtToken"];
            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            var model = new ReservationCreateViewModel();

            try
            {
                // Fetch opening hours
                var openingHoursResponse = await _client.GetAsync($"{_baseUri}api/OpeningHours/all");
                if (openingHoursResponse.IsSuccessStatusCode)
                {
                    var openingHoursJson = await openingHoursResponse.Content.ReadAsStringAsync();
                    ViewBag.OpeningHoursJson = openingHoursJson;

                    // Deserialize the JSON to get the closed days
                    var openingHours = JsonConvert.DeserializeObject<List<OpeningHoursViewModel>>(openingHoursJson);
                    var closedDays = openingHours?
                        .Where(oh => oh.IsClosed)
                        .Select(oh => oh.DayOfWeek)
                        .ToList();

                    // Serialize the closedDays back to JSON (to be used in Reservation.js)
                    var closedDaysJson = JsonConvert.SerializeObject(closedDays);
                    ViewBag.ClosedDaysJson = closedDaysJson;
                }
                else
                {
                    _logger.LogError("Failed to fetch opening hours.");
                    ViewData["ResponseError"] = "Failed to fetch opening hours. Please try again later.";
                }

                // Fetch tables
                var tablesResponse = await _client.GetAsync($"{_baseUri}api/Table/all");
                if (tablesResponse.IsSuccessStatusCode)
                {
                    var tablesJson = await tablesResponse.Content.ReadAsStringAsync();
                    ViewBag.TablesJson = tablesJson;
                }
                else
                {
                    _logger.LogError("Failed to fetch tables.");
                    ViewData["ResponseError"] = "Failed to fetch tables. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching data for reservation creation.");
                ViewData["ResponseError"] = "An error occurred. Please try again later.";
            }

            return View(model);
        }



        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(ReservationCreateViewModel reservationCreateViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(reservationCreateViewModel);
            }

            try
            {
                var json = JsonConvert.SerializeObject(reservationCreateViewModel);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Sending reservation request: {json}");

                var response = await _client.PostAsync($"{_baseUri}api/Reservation/create", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Received response: Status: {response.StatusCode}, Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var createdReservation = JsonConvert.DeserializeObject<ReservationDetailsViewModel>(responseContent);
                    TempData["SuccessMessage"] = $"Successfully created new reservation for {createdReservation?.Customer?.FirstName} {createdReservation?.Customer?.LastName} with ID: {createdReservation?.ReservationId}";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", $"Error creating reservation. Status code: {response.StatusCode}. Response: {responseContent}");
                    return View(reservationCreateViewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the reservation");
                ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
                return View(reservationCreateViewModel);
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetReservationsForDate(DateTime date)
        {
            try
            {
                var response = await _client.GetAsync($"{_baseUri}api/Reservation/date/{date:yyyy-MM-dd}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var reservations = JsonConvert.DeserializeObject<List<ReservationDetailsViewModel>>(jsonResponse);
                    return Json(reservations);
                }
                return BadRequest("Failed to fetch reservations.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reservations for the selected date.");
                return BadRequest("An error occurred while fetching reservations.");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAvailableTables()
        {
            try
            {
                var response = await _client.GetAsync($"{_baseUri}api/Table/all");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var tables = JsonConvert.DeserializeObject<List<TableViewModel>>(jsonResponse);
                    return Json(tables);
                }
                return BadRequest("Failed to fetch tables.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tables.");
                return BadRequest("An error occurred while fetching the tables.");
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAllTables()
        {
            try
            {
                var response = await _client.GetAsync($"{_baseUri}api/Table/all");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var tables = JsonConvert.DeserializeObject<List<TableViewModel>>(json);
                    return Json(tables);
                }
                return BadRequest("Failed to fetch tables");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tables");
                return BadRequest("An error occurred while fetching tables");
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetOpeningHours()
        {
            try
            {
                var response = await _client.GetAsync($"{_baseUri}api/OpeningHours/all");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var openingHours = JsonConvert.DeserializeObject<List<OpeningHoursViewModel>>(json);
                    return Json(openingHours);
                }
                return BadRequest("Failed to fetch opening hours");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching opening hours");
                return BadRequest("An error occurred while fetching opening hours");
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

            var response = await _client.GetAsync($"{_baseUri}api/Reservation/{id}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var reservation = JsonConvert.DeserializeObject<ReservationDetailsViewModel>(jsonResponse);

                return View(reservation);
            }

            return NotFound();
        }



        [Authorize]
        // MUST name the parameter "id" and not "reservationId" because ASP.NET Core's routing system
        // sets a route value with the key "id" when the asp-route-id attribute is used in the HTML of
        // the View where the Edit controller is called from.
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit reservation";


            var token = HttpContext.Request.Cookies["jwtToken"];

            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                var response = await _client.GetAsync($"{_baseUri}api/OpeningHours/all");
                if (response.IsSuccessStatusCode)
                {
                    var openingHoursJson = await response.Content.ReadAsStringAsync();
                    var openingHours = JsonConvert.DeserializeObject<List<OpeningHoursViewModel>>(openingHoursJson);

                    // Calculate days of the week that should be greyed out
                    var closedDays = openingHours
                        .Where(oh => oh.IsClosed)
                        .Select(oh => oh.DayOfWeek)
                        .ToList();

                    ViewBag.ClosedDays = closedDays;
                    ViewBag.OpeningHours = openingHours; // Pass the full OpeningHours data to the view
                }
                else
                {
                    _logger.LogError("Failed to fetch opening hours.");
                    ViewData["ResponseError"] = "Failed to fetch opening hours. Please try again later.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching opening hours.");
                ViewData["ResponseError"] = "An error occurred. Please try again later.";
            }

            try
            {
                var response = await _client.GetAsync($"{_baseUri}api/Reservation/{id}");

                var json = await response.Content.ReadAsStringAsync();

                var reservation = JsonConvert.DeserializeObject<ReservationDetailsViewModel>(json);

                var reservationUpdateViewModel = new ReservationUpdateViewModel
                {
                    ReservationId = id,
                    CustomerId = reservation.Customer.CustomerId,
                    NumberOfGuests = reservation.NumberOfGuests,
                    ReservationDate = reservation.ReservationDate,
                    TableNumbers = reservation.Tables.Select(t => t.TableNumber).ToList(),
                    SpecialRequests = reservation.SpecialRequests
                };

                return View(reservationUpdateViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured while fetching the reservation.");
                ViewData["ResponseError"] = "An error occured. Please try again later.";
            }

            return View();
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(ReservationUpdateViewModel reservationEditViewModel)
        {
            // Post/Redirect/Get (PRG) Pattern with TempData

            var json = JsonConvert.SerializeObject(reservationEditViewModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PutAsync($"{_baseUri}api/Reservation/{reservationEditViewModel.ReservationId}", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var updatedReservation = JsonConvert.DeserializeObject<ReservationDetailsViewModel>(jsonResponse);

                TempData["SuccessMessage"] =
                    $"Successfully created new reservation for " +
                    $"{updatedReservation?.Customer?.FirstName} " +
                    $"{updatedReservation?.Customer?.LastName} " +
                    $"with ID: {updatedReservation?.ReservationId}";

                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Error creating reservation.");
            return View(reservationEditViewModel);
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

            var response = await _client.DeleteAsync($"{_baseUri}api/Reservation/{id}");

            return RedirectToAction("Index");
        }
    }
}
