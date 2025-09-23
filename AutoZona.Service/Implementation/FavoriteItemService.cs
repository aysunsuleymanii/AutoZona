using AutoZona.Domain.DomainModels;
using AutoZona.Repository;
using AutoZona.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoZona.Service.Implementation;

public class FavoriteItemService : IFavoriteItemService
{
    private readonly IRepository<FavoriteList> _favoriteListRepository;
    private readonly IRepository<FavoriteItem> _favoriteItemRepository;
    private readonly IRepository<CarListing> _carRepository;
    private readonly ILogger<FavoriteItemService> _logger;

    public FavoriteItemService(
        IRepository<FavoriteList> favoriteListRepository,
        IRepository<FavoriteItem> favoriteItemRepository,
        IRepository<CarListing> carRepository,
        ILogger<FavoriteItemService> logger)
    {
        _favoriteListRepository = favoriteListRepository;
        _favoriteItemRepository = favoriteItemRepository;
        _carRepository = carRepository;
        _logger = logger;
    }


    public async Task<FavoriteList> CreateFavoritesListAsync(string name, string? description, string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Favorites list name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            var favoritesList = new FavoriteList
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Description = description?.Trim(),
                ListOwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            var result = _favoriteListRepository.Insert(favoritesList);
            _logger.LogInformation(
                "Favorites list '{ListName}' created successfully with ID {ListId} for user {UserId}",
                name, result.Id, userId);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating favorites list for user {UserId}", userId);
            throw;
        }
    }

    public async Task<FavoriteList?> GetFavoritesListByIdAsync(Guid listId, string userId)
    {
        try
        {
            if (listId == Guid.Empty)
                throw new ArgumentException("List ID cannot be empty", nameof(listId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            var result = _favoriteListRepository.Get(
                selector: fl => fl,
                predicate: fl => fl.Id == listId && fl.ListOwnerId == userId,
                include: query => query
                    .Include(fl => fl.FavoriteItems)
                    .ThenInclude(fi => fi.CarListing)
                    .ThenInclude(cl => cl != null ? cl.Images : null)
                    .Include(fl => fl.FavoriteItems)
                    .ThenInclude(fi => fi.CarListing)
                    .ThenInclude(cl => cl != null ? cl.ListingOwner : null)
            );

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving favorites list {ListId} for user {UserId}", listId, userId);
            throw;
        }
    }

    public async Task<IEnumerable<FavoriteList>> GetUserFavoritesListsAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            var result = _favoriteListRepository.GetAll(
                selector: fl => fl,
                predicate: fl => fl.ListOwnerId == userId,
                orderBy: query => query.OrderByDescending(fl => fl.CreatedAt),
                include: query => query
                    .Include(fl => fl.FavoriteItems)
                    .ThenInclude(fi => fi.CarListing)
                    .ThenInclude(cl => cl != null ? cl.Images : null)
                    .Include(fl => fl.FavoriteItems)
                    .ThenInclude(fi => fi.CarListing)
                    .ThenInclude(cl => cl != null ? cl.ListingOwner : null)
            );

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving favorites lists for user {UserId}", userId);
            throw;
        }
    }

    public async Task<FavoriteList> UpdateFavoritesListAsync(Guid listId, string name, string? description,
        string userId)
    {
        try
        {
            if (listId == Guid.Empty)
                throw new ArgumentException("List ID cannot be empty", nameof(listId));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Favorites list name cannot be empty", nameof(name));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            var favoritesList = _favoriteListRepository.Get(
                selector: fl => fl,
                predicate: fl => fl.Id == listId && fl.ListOwnerId == userId
            );

            if (favoritesList == null)
                throw new ArgumentException($"Favorites list with ID {listId} not found or access denied");

            favoritesList.Name = name.Trim();
            favoritesList.Description = description?.Trim();

            var result = _favoriteListRepository.Update(favoritesList);
            _logger.LogInformation("Favorites list {ListId} updated successfully for user {UserId}", listId, userId);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating favorites list {ListId} for user {UserId}", listId, userId);
            throw;
        }
    }

    public async Task<bool> DeleteFavoritesListAsync(Guid listId, string userId)
    {
        try
        {
            if (listId == Guid.Empty)
                throw new ArgumentException("List ID cannot be empty", nameof(listId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            var favoritesList = _favoriteListRepository.Get(
                selector: fl => fl,
                predicate: fl => fl.Id == listId && fl.ListOwnerId == userId
            );

            if (favoritesList == null)
                return false;

            _favoriteListRepository.Delete(favoritesList);
            _logger.LogInformation("Favorites list {ListId} deleted successfully for user {UserId}", listId, userId);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting favorites list {ListId} for user {UserId}", listId, userId);
            throw;
        }
    }


    public async Task<bool> AddCarToFavoritesAsync(Guid listId, Guid carId, string userId)
    {
        try
        {
            if (listId == Guid.Empty)
                throw new ArgumentException("List ID cannot be empty", nameof(listId));

            if (carId == Guid.Empty)
                throw new ArgumentException("Car ID cannot be empty", nameof(carId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            // Check if the list belongs to the user
            var favoritesList = _favoriteListRepository.Get(
                selector: fl => fl,
                predicate: fl => fl.Id == listId && fl.ListOwnerId == userId
            );

            if (favoritesList == null)
            {
                _logger.LogWarning(
                    "Attempt to add car to non-existent or unauthorized favorites list {ListId} by user {UserId}",
                    listId, userId);
                return false;
            }

            // Check if the car exists and is active
            var car = _carRepository.Get(
                selector: c => c,
                predicate: c => c.Id == carId && c.IsActive
            );

            if (car == null)
            {
                _logger.LogWarning("Attempt to add non-existent or inactive car {CarId} to favorites by user {UserId}",
                    carId, userId);
                return false;
            }

            // Check if the car is already in the favorites list
            var existingItem = _favoriteItemRepository.Get(
                selector: fi => fi,
                predicate: fi => fi.FavoriteListId == listId && fi.CarListingId == carId
            );

            if (existingItem != null)
            {
                _logger.LogInformation("Car {CarId} already exists in favorites list {ListId} for user {UserId}", carId,
                    listId, userId);
                return false; // Already exists
            }

            // Add the car to favorites
            var favoriteItem = new FavoriteItem
            {
                Id = Guid.NewGuid(),
                FavoriteListId = listId,
                CarListingId = carId,
                AddedAt = DateTime.UtcNow
            };

            _favoriteItemRepository.Insert(favoriteItem);
            _logger.LogInformation("Car {CarId} added to favorites list {ListId} for user {UserId}", carId, listId,
                userId);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding car {CarId} to favorites list {ListId} for user {UserId}", carId, listId,
                userId);
            throw;
        }
    }

    public async Task<bool> RemoveCarFromFavoritesAsync(Guid listId, Guid carId, string userId)
    {
        try
        {
            if (listId == Guid.Empty)
                throw new ArgumentException("List ID cannot be empty", nameof(listId));

            if (carId == Guid.Empty)
                throw new ArgumentException("Car ID cannot be empty", nameof(carId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            // Check if the list belongs to the user and get the favorite item
            var favoriteItem = _favoriteItemRepository.Get(
                selector: fi => fi,
                predicate: fi => fi.FavoriteListId == listId &&
                                 fi.CarListingId == carId &&
                                 fi.FavoriteList != null &&
                                 fi.FavoriteList.ListOwnerId == userId,
                include: query => query.Include(fi => fi.FavoriteList)
            );

            if (favoriteItem == null)
            {
                _logger.LogWarning(
                    "Attempt to remove non-existent favorite item or unauthorized access. Car {CarId}, List {ListId}, User {UserId}",
                    carId, listId, userId);
                return false;
            }

            _favoriteItemRepository.Delete(favoriteItem);
            _logger.LogInformation("Car {CarId} removed from favorites list {ListId} for user {UserId}", carId, listId,
                userId);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing car {CarId} from favorites list {ListId} for user {UserId}", carId,
                listId, userId);
            throw;
        }
    }

    public async Task<bool> IsCarInFavoritesAsync(Guid listId, Guid carId, string userId)
    {
        try
        {
            if (listId == Guid.Empty)
                throw new ArgumentException("List ID cannot be empty", nameof(listId));

            if (carId == Guid.Empty)
                throw new ArgumentException("Car ID cannot be empty", nameof(carId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            var favoriteItem = _favoriteItemRepository.Get(
                selector: fi => fi.Id,
                predicate: fi => fi.FavoriteListId == listId &&
                                 fi.CarListingId == carId &&
                                 fi.FavoriteList != null &&
                                 fi.FavoriteList.ListOwnerId == userId,
                include: query => query.Include(fi => fi.FavoriteList)
            );

            return await Task.FromResult(favoriteItem != Guid.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if car {CarId} is in favorites list {ListId} for user {UserId}", carId,
                listId, userId);
            throw;
        }
    }

    public async Task<IEnumerable<FavoriteList>> GetFavoritesListsContainingCarAsync(Guid carId, string userId)
    {
        try
        {
            if (carId == Guid.Empty)
                throw new ArgumentException("Car ID cannot be empty", nameof(carId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            var result = _favoriteListRepository.GetAll(
                selector: fl => fl,
                predicate: fl => fl.ListOwnerId == userId &&
                                 fl.FavoriteItems.Any(fi => fi.CarListingId == carId),
                orderBy: query => query.OrderBy(fl => fl.Name),
                include: query => query.Include(fl => fl.FavoriteItems)
            );

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving favorites lists containing car {CarId} for user {UserId}", carId,
                userId);
            throw;
        }
    }


    public async Task<int> GetFavoritesCountAsync(Guid listId)
    {
        try
        {
            if (listId == Guid.Empty)
                throw new ArgumentException("List ID cannot be empty", nameof(listId));

            var count = _favoriteItemRepository.GetAll(
                selector: fi => fi.Id,
                predicate: fi => fi.FavoriteListId == listId
            ).Count();

            return await Task.FromResult(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorites count for list {ListId}", listId);
            throw;
        }
    }

    public async Task<bool> IsOwnerOfFavoritesListAsync(Guid listId, string userId)
    {
        try
        {
            if (listId == Guid.Empty)
                throw new ArgumentException("List ID cannot be empty", nameof(listId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            var ownerId = _favoriteListRepository.Get(
                selector: fl => fl.ListOwnerId,
                predicate: fl => fl.Id == listId
            );

            return await Task.FromResult(ownerId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ownership of favorites list {ListId} for user {UserId}", listId,
                userId);
            throw;
        }
    }


    public async Task<int> GetUserTotalFavoritesCountAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            var count = _favoriteItemRepository.GetAll(
                selector: fi => fi.Id,
                predicate: fi => fi.FavoriteList != null && fi.FavoriteList.ListOwnerId == userId,
                include: query => query.Include(fi => fi.FavoriteList)
            ).Count();

            return await Task.FromResult(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total favorites count for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<CarListing>> GetFavoriteItemsFromListAsync(Guid listId, string userId)
    {
        try
        {
            if (listId == Guid.Empty)
                throw new ArgumentException("List ID cannot be empty", nameof(listId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            // First verify the user owns the list
            var isOwner = await IsOwnerOfFavoritesListAsync(listId, userId);
            if (!isOwner)
                return Enumerable.Empty<CarListing>();

            var favoriteItems = _favoriteItemRepository.GetAll(
                    selector: fi => fi.CarListing,
                    predicate: fi => fi.FavoriteListId == listId &&
                                     fi.CarListing != null &&
                                     fi.CarListing.IsActive,
                    orderBy: query => query.OrderByDescending(fi => fi.AddedAt),
                    include: query => query
                        .Include(fi => fi.CarListing)
                        .ThenInclude(cl => cl != null ? cl.Images : null)
                        .Include(fi => fi.CarListing)
                        .ThenInclude(cl => cl != null ? cl.ListingOwner : null)
                ).Where(car => car != null)
                .Cast<CarListing>();

            return await Task.FromResult(favoriteItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting favorite items from list {ListId} for user {UserId}", listId, userId);
            throw;
        }
    }


    public async Task<FavoriteList> GetOrCreateDefaultFavoritesListAsync(string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            // Check if user has any favorites lists
            var existingLists = await GetUserFavoritesListsAsync(userId);
            var defaultList = existingLists.FirstOrDefault();

            // If no lists exist, create a default one
            if (defaultList == null)
            {
                defaultList = await CreateFavoritesListAsync("My Favorites", "My favorite cars", userId);
                _logger.LogInformation("Created default favorites list for user {UserId}", userId);
            }

            return defaultList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating default favorites list for user {UserId}", userId);
            throw;
        }
    }
}