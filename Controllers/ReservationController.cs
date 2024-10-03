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

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Reservations";

            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.MenuItemCreatedMessage = TempData["SuccessMessage"].ToString();
                TempData.Clear();
            }

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

            return View(model);
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

            ModelState.AddModelError("", "Error creating reservation.");
            return View(reservationCreateViewModel);
        }


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



        //[HttpGet] // Old one, used for fetching pre-created TimeSlots
        //public async Task<IActionResult> GetAvailableTimeSlots(DateTime date, int numberOfGuests)
        //{
        //    if (numberOfGuests <= 0)
        //    {
        //        return BadRequest("Number of guests must be greater than 0.");
        //    }

        //    try
        //    {
        //        var timeSlotResponse = await _client.GetAsync($"{_baseUri}api/TimeSlot/daily/{date:yyyy-MM-dd}");
        //        if (!timeSlotResponse.IsSuccessStatusCode)
        //        {
        //            return BadRequest("Failed to fetch available time slots");
        //        }

        //        var timeSlotJson = await timeSlotResponse.Content.ReadAsStringAsync();
        //        var timeSlots = JsonConvert.DeserializeObject<List<TimeSlotViewModel>>(timeSlotJson);

        //        var tableResponse = await _client.GetAsync($"{_baseUri}api/Table/all");
        //        if (!tableResponse.IsSuccessStatusCode)
        //        {
        //            return BadRequest("Failed to fetch tables");
        //        }

        //        var tableJson = await tableResponse.Content.ReadAsStringAsync();
        //        var tables = JsonConvert.DeserializeObject<List<TableViewModel>>(tableJson);

        //        var enrichedTimeSlots = timeSlots
        //            .Where(slot => slot.ReservationId == null)
        //            .Select(slot =>
        //            {
        //                var table = tables.FirstOrDefault(t => t.TableId == slot.TableId);
        //                if (table != null && table.SeatingCapacity >= numberOfGuests)
        //                {
        //                    slot.TableViewModel = table;
        //                }
        //                return slot;
        //            })
        //            .Where(slot => slot.TableViewModel != null)
        //            .ToList();

        //        if (!enrichedTimeSlots.Any())
        //        {
        //            return Json(new { message = "No available time slots for the selected date" });
        //        }

        //        return Json(enrichedTimeSlots);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error fetching available time slots");
        //        return BadRequest("An error occurred while fetching available time slots");
        //    }
        //}




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
                return BadRequest("An error occured while fetching tables");
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
