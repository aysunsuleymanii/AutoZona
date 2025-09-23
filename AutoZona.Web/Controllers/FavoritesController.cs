using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AutoZona.Domain.DomainModels;
using AutoZona.Domain.IdentityModels;
using AutoZona.Service.Interface;
using System.Text.Json;

namespace AutoZona.Web.Controllers
{
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly IFavoriteItemService _favoriteItemService;
        private readonly UserManager<AutoZonaApplicationUser> _userManager;
        private readonly ILogger<FavoritesController> _logger;

        public FavoritesController(
            IFavoriteItemService favoriteItemService,
            UserManager<AutoZonaApplicationUser> userManager,
            ILogger<FavoritesController> logger)
        {
            _favoriteItemService = favoriteItemService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Favorites
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var favoritesLists = await _favoriteItemService.GetUserFavoritesListsAsync(user.Id);
                return View(favoritesLists.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorites lists");
                TempData["ErrorMessage"] = "An error occurred while loading your favorites.";
                return View(new List<FavoriteList>());
            }
        }

        // GET: Favorites/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var favoritesList = await _favoriteItemService.GetFavoritesListByIdAsync(id, user.Id);
                if (favoritesList == null)
                {
                    return NotFound();
                }

                return View(favoritesList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorites list details {ListId}", id);
                return NotFound();
            }
        }

        // GET: Favorites/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Favorites/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name, string? description)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    ModelState.AddModelError("Name", "List name is required");
                    return View();
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                await _favoriteItemService.CreateFavoritesListAsync(name, description, user.Id);
                TempData["SuccessMessage"] = "Favorites list created successfully!";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating favorites list");
                TempData["ErrorMessage"] = "An error occurred while creating the favorites list.";
                return View();
            }
        }

        // GET: Favorites/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var favoritesList = await _favoriteItemService.GetFavoritesListByIdAsync(id, user.Id);
                if (favoritesList == null)
                {
                    return NotFound();
                }

                return View(favoritesList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorites list for edit {ListId}", id);
                return NotFound();
            }
        }

        // POST: Favorites/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, string name, string? description)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    ModelState.AddModelError("Name", "List name is required");
                    // Reload the list for the view
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        var favoritesList = await _favoriteItemService.GetFavoritesListByIdAsync(id, user.Id);
                        return View(favoritesList);
                    }
                    return View();
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                await _favoriteItemService.UpdateFavoritesListAsync(id, name, description, currentUser.Id);
                TempData["SuccessMessage"] = "Favorites list updated successfully!";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating favorites list {ListId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the favorites list.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Favorites/Delete/5
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var favoritesList = await _favoriteItemService.GetFavoritesListByIdAsync(id, user.Id);
                if (favoritesList == null)
                {
                    return NotFound();
                }

                return View(favoritesList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading favorites list for delete {ListId}", id);
                return NotFound();
            }
        }

        // POST: Favorites/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var result = await _favoriteItemService.DeleteFavoritesListAsync(id, user.Id);
                if (result)
                {
                    TempData["SuccessMessage"] = "Favorites list deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "The favorites list could not be found.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting favorites list {ListId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the favorites list.";
                return RedirectToAction(nameof(Index));
            }
        }

        // AJAX: Add car to favorites
        [HttpPost]
        public async Task<IActionResult> AddToFavorites([FromBody] AddToFavoritesRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = await _favoriteItemService.AddCarToFavoritesAsync(request.ListId, request.CarId, user.Id);
                
                if (result)
                {
                    return Json(new { success = true, message = "Car added to favorites successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Car is already in this favorites list or an error occurred." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding car {CarId} to favorites list {ListId}", request.CarId, request.ListId);
                return Json(new { success = false, message = "An error occurred while adding the car to favorites." });
            }
        }

        // AJAX: Remove car from favorites
        [HttpPost]
        public async Task<IActionResult> RemoveFromFavorites([FromBody] RemoveFromFavoritesRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = await _favoriteItemService.RemoveCarFromFavoritesAsync(request.ListId, request.CarId, user.Id);
                
                if (result)
                {
                    return Json(new { success = true, message = "Car removed from favorites successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Car not found in favorites or an error occurred." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing car {CarId} from favorites list {ListId}", request.CarId, request.ListId);
                return Json(new { success = false, message = "An error occurred while removing the car from favorites." });
            }
        }

        // AJAX: Get user's favorites lists for dropdown
        [HttpGet]
        public async Task<IActionResult> GetUserFavoritesLists()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, lists = new object[0] });
                }

                var favoritesLists = await _favoriteItemService.GetUserFavoritesListsAsync(user.Id);
                var listsData = favoritesLists.Select(fl => new
                {
                    id = fl.Id,
                    name = fl.Name,
                    itemCount = fl.FavoriteItems.Count
                }).ToList();

                return Json(new { success = true, lists = listsData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user favorites lists");
                return Json(new { success = false, lists = new object[0] });
            }
        }
        // AJAX: Check if car is in any favorites list
        [HttpGet]
        public async Task<IActionResult> CheckCarInFavorites(Guid carId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, inFavorites = false, lists = new object[0] });
                }

                var listsContainingCar = await _favoriteItemService.GetFavoritesListsContainingCarAsync(carId, user.Id);
                var listsData = listsContainingCar.Select(fl => new
                {
                    id = fl.Id,
                    name = fl.Name
                }).ToList();

                return Json(new { 
                    success = true, 
                    inFavorites = listsData.Any(), 
                    lists = listsData 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if car {CarId} is in favorites", carId);
                return Json(new { success = false, inFavorites = false, lists = new object[0] });
            }
        }
    }
    

    // DTOs for AJAX requests
    public class AddToFavoritesRequest
    {
        public Guid ListId { get; set; }
        public Guid CarId { get; set; }
    }

    public class RemoveFromFavoritesRequest
    {
        public Guid ListId { get; set; }
        public Guid CarId { get; set; }
    }
}