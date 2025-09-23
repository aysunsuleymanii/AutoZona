using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AutoZona.Domain.DomainModels;
using AutoZona.Domain.IdentityModels;
using AutoZona.Domain.Enums;
using AutoZona.Service.Interface;

namespace AutoZona.Web.Controllers
{
    public class CarListingsController : Controller
    {
        private readonly ICarListingService _carService;
        private readonly IFavoriteItemService _favoritesService;
        private readonly UserManager<AutoZonaApplicationUser> _userManager;
        private readonly ILogger<CarListingsController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CarListingsController(
            ICarListingService carService,
            IFavoriteItemService favoritesService,
            UserManager<AutoZonaApplicationUser> userManager,
            ILogger<CarListingsController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _carService = carService;
            _favoritesService = favoritesService;
            _userManager = userManager;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: CarListings
        public async Task<IActionResult> Index(
            string? make = null,
            string? model = null,
            int? yearFrom = null,
            int? yearTo = null,
            decimal? priceFrom = null,
            decimal? priceTo = null,
            int? maxMileage = null,
            FuelType? fuel = null,
            BodyType? bodyType = null,
            Transmission? transmission = null,
            Color? color = null,
            string? city = null,
            string? sortBy = "created",
            string? sortOrder = "desc",
            int page = 1)
        {
            try
            {
                const int pageSize = 12;

                var (cars, totalCount) = await _carService.GetCarsWithPaginationAsync(
                    page, pageSize, make, model, yearFrom, yearTo, priceFrom, priceTo,
                    maxMileage, fuel, bodyType, transmission, color, city, sortBy, sortOrder);

                // Get filter data for dropdowns
                ViewBag.Makes = await GetMakesSelectList();
                ViewBag.Models = await GetModelsSelectList(make);
                ViewBag.Years = GetYearsSelectList();
                ViewBag.FuelTypes = GetFuelTypesSelectList();
                ViewBag.BodyTypes = GetBodyTypesSelectList();
                ViewBag.Transmissions = GetTransmissionsSelectList();
                ViewBag.Colors = GetColorsSelectList();

                // Pagination info
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                ViewBag.TotalCount = totalCount;
                ViewBag.HasPreviousPage = page > 1;
                ViewBag.HasNextPage = page < ViewBag.TotalPages;

                // Current filter values
                ViewBag.CurrentMake = make;
                ViewBag.CurrentModel = model;
                ViewBag.CurrentYearFrom = yearFrom;
                ViewBag.CurrentYearTo = yearTo;
                ViewBag.CurrentPriceFrom = priceFrom;
                ViewBag.CurrentPriceTo = priceTo;
                ViewBag.CurrentMaxMileage = maxMileage;
                ViewBag.CurrentFuel = fuel;
                ViewBag.CurrentBodyType = bodyType;
                ViewBag.CurrentTransmission = transmission;
                ViewBag.CurrentColor = color;
                ViewBag.CurrentCity = city;
                ViewBag.CurrentSortBy = sortBy;
                ViewBag.CurrentSortOrder = sortOrder;

                return View(cars.ToList()); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car listings index page");
                TempData["ErrorMessage"] = "An error occurred while loading the car listings.";
                return View(new List<CarListing>());
            }
        }

        // GET: CarListings/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var carListing = await _carService.GetCarListingByIdAsync(id.Value);
                if (carListing == null)
                {
                    return NotFound();
                }

                // Check if current user has this car in favorites (if logged in)
                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var userFavoritesLists = await _favoritesService.GetUserFavoritesListsAsync(user.Id);
                        ViewBag.UserFavoritesLists = userFavoritesLists;

                        var favoritesListsContainingCar =
                            await _favoritesService.GetFavoritesListsContainingCarAsync(id.Value, user.Id);
                        ViewBag.IsInFavorites = favoritesListsContainingCar.Any();
                        ViewBag.FavoritesListsContainingCar = favoritesListsContainingCar;
                    }
                }

                return View(carListing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car listing details for ID {CarId}", id);
                return NotFound();
            }
        }

        // GET: CarListings/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            ViewBag.Makes = await GetMakesSelectList();
            ViewBag.FuelTypes = GetFuelTypesSelectList();
            ViewBag.BodyTypes = GetBodyTypesSelectList();
            ViewBag.Transmissions = GetTransmissionsSelectList();
            ViewBag.Colors = GetColorsSelectList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(CarListing carListing, List<IFormFile> PhotoFiles)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null)
                    {
                        return Unauthorized();
                    }

                    carListing.ListingOwnerId = user.Id;
                    carListing.CreatedAt = DateTime.UtcNow;
                    carListing.UpdatedAt = DateTime.UtcNow;
                    carListing.IsActive = true;

                    // Process uploaded photos
                    if (PhotoFiles != null && PhotoFiles.Any())
                    {
                        await ProcessCarPhotos(carListing, PhotoFiles);
                    }

                    await _carService.CreateCarListingAsync(carListing);

                    TempData["SuccessMessage"] = "Your car listing has been created successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating car listing");
                TempData["ErrorMessage"] = "An error occurred while creating your listing. Please try again.";
            }

            // Reload dropdowns if validation fails
            ViewBag.Makes = await GetMakesSelectList();
            ViewBag.FuelTypes = GetFuelTypesSelectList();
            ViewBag.BodyTypes = GetBodyTypesSelectList();
            ViewBag.Transmissions = GetTransmissionsSelectList();
            ViewBag.Colors = GetColorsSelectList();

            return View(carListing);
        }

        private async Task ProcessCarPhotos(CarListing carListing, List<IFormFile> photoFiles)
        {
            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "cars");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            const long maxFileSize = 5 * 1024 * 1024; // 5MB

            for (int i = 0; i < Math.Min(photoFiles.Count, 10); i++) // Limit to 10 photos
            {
                var file = photoFiles[i];

                if (file == null || file.Length == 0)
                    continue;

                // Validate file
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("Invalid file extension: {Extension}", extension);
                    continue;
                }

                if (file.Length > maxFileSize)
                {
                    _logger.LogWarning("File too large: {Size} bytes", file.Length);
                    continue;
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create CarImage entity
                var carImage = new CarImage
                {
                    Id = Guid.NewGuid(),
                    CarListingId = carListing.Id,
                    ImageUrl = $"/uploads/cars/{fileName}",
                    IsPrimary = i == 0,
                };

                carListing.Images.Add(carImage);
            }
        }

        // GET: CarListings/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var carListing = await _carService.GetCarListingByIdAsync(id.Value);
                if (carListing == null)
                {
                    return NotFound();
                }

                // Check if current user owns this listing
                var user = await _userManager.GetUserAsync(User);
                if (user == null || !await _carService.IsOwnerOfCarAsync(id.Value, user.Id))
                {
                    return Forbid();
                }

                ViewBag.Makes = await GetMakesSelectList();
                ViewBag.FuelTypes = GetFuelTypesSelectList();
                ViewBag.BodyTypes = GetBodyTypesSelectList();
                ViewBag.Transmissions = GetTransmissionsSelectList();
                ViewBag.Colors = GetColorsSelectList();

                return View(carListing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car listing for edit, ID {CarId}", id);
                return NotFound();
            }
        }

        // POST: CarListings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Edit(Guid id, CarListing carListing)
        {
            if (id != carListing.Id)
            {
                return NotFound();
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || !await _carService.IsOwnerOfCarAsync(id, user.Id))
                {
                    return Forbid();
                }

                if (ModelState.IsValid)
                {
                    carListing.UpdatedAt = DateTime.UtcNow;
                    await _carService.UpdateCarListingAsync(carListing);

                    TempData["SuccessMessage"] = "Your car listing has been updated successfully!";
                    return RedirectToAction(nameof(MyListings));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating car listing {CarId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating your listing. Please try again.";
            }

            // Reload dropdowns if validation fails
            ViewBag.Makes = await GetMakesSelectList();
            ViewBag.FuelTypes = GetFuelTypesSelectList();
            ViewBag.BodyTypes = GetBodyTypesSelectList();
            ViewBag.Transmissions = GetTransmissionsSelectList();
            ViewBag.Colors = GetColorsSelectList();

            return View(carListing);
        }

        // GET: CarListings/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var carListing = await _carService.GetCarListingByIdAsync(id.Value);
                if (carListing == null)
                {
                    return NotFound();
                }

                // Check if current user owns this listing
                var user = await _userManager.GetUserAsync(User);
                if (user == null || !await _carService.IsOwnerOfCarAsync(id.Value, user.Id))
                {
                    return Forbid();
                }

                return View(carListing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading car listing for delete confirmation, ID {CarId}", id);
                return NotFound();
            }
        }

        // POST: CarListings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || !await _carService.IsOwnerOfCarAsync(id, user.Id))
                {
                    return Forbid();
                }

                var result = await _carService.DeleteCarListingAsync(id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Your car listing has been deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "The listing could not be found or has already been deleted.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting car listing {CarId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting your listing. Please try again.";
            }

            return RedirectToAction(nameof(MyListings));
        }

        // GET: CarListings/MyListings
        [Authorize]
        public async Task<IActionResult> MyListings()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var userListings = await _carService.GetUserCarListingsAsync(user.Id);
                return View(userListings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user's car listings");
                TempData["ErrorMessage"] = "An error occurred while loading your listings.";
                return View(new List<CarListing>());
            }
        }

        // AJAX: Get models for a specific make
        [HttpGet]
        public async Task<IActionResult> GetModelsByMake(string make)
        {
            try
            {
                if (string.IsNullOrEmpty(make))
                {
                    return Json(new List<string>());
                }

                var models = await _carService.GetAvailableModelsAsync(make);
                return Json(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting models for make {Make}", make);
                return Json(new List<string>());
            }
        }

        #region Helper Methods

        private async Task<SelectList> GetMakesSelectList()
        {
            var makes = await _carService.GetAvailableMakesAsync();
            return new SelectList(makes.Select(m => new { Value = m, Text = m }), "Value", "Text");
        }

        private async Task<SelectList> GetModelsSelectList(string? selectedMake = null)
        {
            if (string.IsNullOrEmpty(selectedMake))
            {
                return new SelectList(new List<string>());
            }

            var models = await _carService.GetAvailableModelsAsync(selectedMake);
            return new SelectList(models.Select(m => new { Value = m, Text = m }), "Value", "Text");
        }

        private SelectList GetYearsSelectList()
        {
            var currentYear = DateTime.Now.Year;
            var years = Enumerable.Range(1990, currentYear - 1990 + 1).Reverse();
            return new SelectList(years.Select(y => new { Value = y, Text = y.ToString() }), "Value", "Text");
        }

        private SelectList GetFuelTypesSelectList()
        {
            var fuelTypes = Enum.GetValues<FuelType>()
                .Select(ft => new { Value = (int)ft, Text = ft.ToString() });
            return new SelectList(fuelTypes, "Value", "Text");
        }

        private SelectList GetBodyTypesSelectList()
        {
            var bodyTypes = Enum.GetValues<BodyType>()
                .Select(bt => new { Value = (int)bt, Text = bt.ToString() });
            return new SelectList(bodyTypes, "Value", "Text");
        }

        private SelectList GetTransmissionsSelectList()
        {
            var transmissions = Enum.GetValues<Transmission>()
                .Select(t => new { Value = (int)t, Text = t.ToString() });
            return new SelectList(transmissions, "Value", "Text");
        }

        private SelectList GetColorsSelectList()
        {
            var colors = Enum.GetValues<Color>()
                .Select(c => new { Value = (int)c, Text = c.ToString() });
            return new SelectList(colors, "Value", "Text");
        }

        #endregion
    }
}