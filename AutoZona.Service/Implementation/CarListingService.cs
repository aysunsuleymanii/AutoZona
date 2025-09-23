using AutoZona.Domain.DomainModels;
using AutoZona.Domain.Enums;
using AutoZona.Repository;
using AutoZona.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoZona.Service.Implementation;

public class CarListingService : ICarListingService
{
    private readonly IRepository<CarListing> _carRepository;
    private readonly IRepository<CarImage> _imageRepository;
    private readonly ILogger<CarListingService> _logger;

    public CarListingService(
        IRepository<CarListing> carRepository,
        IRepository<CarImage> imageRepository,
        ILogger<CarListingService> logger)
    {
        _carRepository = carRepository;
        _imageRepository = imageRepository;
        _logger = logger;
    }


    public async Task<CarListing> CreateCarListingAsync(CarListing carListing)
    {
        try
        {
            carListing.Id = Guid.NewGuid();
            var result = _carRepository.Insert(carListing);
            _logger.LogInformation("Car listing created successfully with ID {CarId} for user {UserId}",
                result.Id, carListing.ListingOwnerId);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating car listing for user {UserId}", carListing.ListingOwnerId);
            throw;
        }
    }

    public async Task<CarListing?> GetCarListingByIdAsync(Guid id)
    {
        try
        {
            var result = _carRepository.Get(
                selector: c => c,
                predicate: c => c.Id == id && c.IsActive,
                include: query => query
                    .Include(c => c.Images.OrderBy(i => i.DisplayOrder))
                    .Include(c => c.ListingOwner)
            );

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving car listing {CarId}", id);
            throw;
        }
    }

    public async Task<CarListing> UpdateCarListingAsync(CarListing carListing)
    {
        try
        {
            carListing.UpdatedAt = DateTime.UtcNow;
            var result = _carRepository.Update(carListing);

            _logger.LogInformation("Car listing {CarId} updated successfully", carListing.Id);
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating car listing {CarId}", carListing.Id);
            throw;
        }
    }

    public async Task<bool> DeleteCarListingAsync(Guid id)
    {
        try
        {
            var carListing = _carRepository.Get(
                selector: c => c,
                predicate: c => c.Id == id && c.IsActive
            );

            if (carListing == null)
                return false;

            // Soft delete
            carListing.IsActive = false;
            carListing.UpdatedAt = DateTime.UtcNow;

            _carRepository.Update(carListing);
            _logger.LogInformation("Car listing {CarId} deleted successfully", id);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting car listing {CarId}", id);
            throw;
        }
    }


    public async Task<IEnumerable<CarListing>> GetAllActiveCarListingsAsync()
    {
        try
        {
            var result = _carRepository.GetAll(
                selector: c => c,
                predicate: c => c.IsActive,
                orderBy: query => query.OrderByDescending(c => c.CreatedAt),
                include: query => query
                    .Include(c => c.Images)
                    .Include(c => c.ListingOwner)
            );

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all active car listings");
            throw;
        }
    }

    public async Task<IEnumerable<CarListing>> SearchCarsAsync(
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
        string? city = null)
    {
        try
        {
            var result = _carRepository.GetAll(
                selector: c => c,
                predicate: c => c.IsActive &&
                                (string.IsNullOrEmpty(make) ||
                                 c.Make != null && c.Make.ToLower().Contains(make.ToLower())) &&
                                (string.IsNullOrEmpty(model) ||
                                 c.Model != null && c.Model.ToLower().Contains(model.ToLower())) &&
                                (!yearFrom.HasValue || c.Year >= yearFrom) &&
                                (!yearTo.HasValue || c.Year <= yearTo) &&
                                (!priceFrom.HasValue || c.Price >= priceFrom) &&
                                (!priceTo.HasValue || c.Price <= priceTo) &&
                                (!maxMileage.HasValue || c.Mileage <= maxMileage) &&
                                (!fuel.HasValue || c.Fuel == fuel) &&
                                (!bodyType.HasValue || c.BodyType == bodyType) &&
                                (!transmission.HasValue || c.Transmission == transmission) &&
                                (!color.HasValue || c.Color == color) &&
                                (string.IsNullOrEmpty(city) || c.ListingOwner != null && c.ListingOwner.City != null &&
                                    c.ListingOwner.City.ToLower().Contains(city.ToLower())),
                orderBy: query => query.OrderByDescending(c => c.CreatedAt),
                include: query => query
                    .Include(c => c.Images)
                    .Include(c => c.ListingOwner)
            );

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching car listings with filters");
            throw;
        }
    }

    public async Task<(IEnumerable<CarListing> cars, int totalCount)> GetCarsWithPaginationAsync(
        int page = 1,
        int pageSize = 20,
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
        string? sortOrder = "desc")
    {
        try
        {
            // Get all filtered results first to count total
            var allFilteredCars = await SearchCarsAsync(make, model, yearFrom, yearTo, priceFrom, priceTo,
                maxMileage, fuel, bodyType, transmission, color, city);

            var totalCount = allFilteredCars.Count();


            var sortedCars = ApplySorting(allFilteredCars.AsQueryable(), sortBy, sortOrder);

            var paginatedCars = sortedCars
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return await Task.FromResult((paginatedCars.AsEnumerable(), totalCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cars with pagination");
            throw;
        }
    }


    public async Task<IEnumerable<CarListing>> GetUserCarListingsAsync(string userId)
    {
        try
        {
            var result = _carRepository.GetAll(
                selector: c => c,
                predicate: c => c.ListingOwnerId == userId,
                orderBy: query => query.OrderByDescending(c => c.CreatedAt),
                include: query => query.Include(c => c.Images)
            );

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving car listings for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsOwnerOfCarAsync(Guid carId, string userId)
    {
        try
        {
            var ownerId = _carRepository.Get(
                selector: c => c.ListingOwnerId,
                predicate: c => c.Id == carId && c.IsActive
            );

            return await Task.FromResult(ownerId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking car ownership for car {CarId} and user {UserId}", carId, userId);
            throw;
        }
    }

    public async Task<int> GetUserActiveListingsCountAsync(string userId)
    {
        try
        {
            var count = _carRepository.GetAll(
                selector: c => c.Id,
                predicate: c => c.ListingOwnerId == userId && c.IsActive
            ).Count();

            return await Task.FromResult(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active listings count for user {UserId}", userId);
            throw;
        }
    }


    public async Task<IEnumerable<string>> GetAvailableMakesAsync()
    {
        try
        {
            var makes = _carRepository.GetAll(
                    selector: c => c.Make,
                    predicate: c => c.IsActive && !string.IsNullOrEmpty(c.Make)
                ).Where(make => !string.IsNullOrEmpty(make))
                .Distinct()
                .OrderBy(make => make);

            return await Task.FromResult(makes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available makes");
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetAvailableModelsAsync(string make)
    {
        try
        {
            var models = _carRepository.GetAll(
                    selector: c => c.Model,
                    predicate: c => c.IsActive &&
                                    !string.IsNullOrEmpty(c.Model) &&
                                    c.Make != null &&
                                    c.Make.ToLower() == make.ToLower()
                ).Where(model => !string.IsNullOrEmpty(model))
                .Distinct()
                .OrderBy(model => model);

            return await Task.FromResult(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available models for make {Make}", make);
            throw;
        }
    }

    public async Task<int> GetTotalActiveListingsCountAsync()
    {
        try
        {
            var count = _carRepository.GetAll(
                selector: c => c.Id,
                predicate: c => c.IsActive
            ).Count();

            return await Task.FromResult(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving total active listings count");
            throw;
        }
    }

    public async Task<decimal> GetAveragePriceAsync()
    {
        try
        {
            var prices = _carRepository.GetAll(
                selector: c => c.Price ?? 0,
                predicate: c => c.IsActive && c.Price.HasValue && c.Price > 0
            );

            if (!prices.Any())
                return 0;

            var average = prices.Average();
            return await Task.FromResult(average);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating average price");
            throw;
        }
    }

    public async Task<IEnumerable<CarListing>> GetRecentListingsAsync(int count = 10)
    {
        try
        {
            var result = _carRepository.GetAll(
                selector: c => c,
                predicate: c => c.IsActive,
                orderBy: query => query.OrderByDescending(c => c.CreatedAt),
                include: query => query
                    .Include(c => c.Images)
                    .Include(c => c.ListingOwner)
            ).Take(count);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent listings");
            throw;
        }
    }

    public async Task<IEnumerable<CarListing>> GetFeaturedListingsAsync()
    {
        try
        {
            var result = _carRepository.GetAll(
                    selector: c => c,
                    predicate: c => c.IsActive,
                    orderBy: query => query.OrderByDescending(c => c.CreatedAt),
                    include: query => query
                        .Include(c => c.Images)
                        .Include(c => c.ListingOwner)
                ).Where(c => c.Images.Any())
                .Take(6); // Featured section typically shows 6 cars

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving featured listings");
            throw;
        }
    }


    public async Task<Dictionary<string, int>> GetMakeStatisticsAsync()
    {
        try
        {
            var makeGroups = _carRepository.GetAll(
                    selector: c => c.Make,
                    predicate: c => c.IsActive && !string.IsNullOrEmpty(c.Make)
                ).Where(make => !string.IsNullOrEmpty(make))
                .GroupBy(make => make)
                .ToDictionary(g => g.Key!, g => g.Count())
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return await Task.FromResult(makeGroups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving make statistics");
            throw;
        }
    }

    public async Task<Dictionary<BodyType, int>> GetBodyTypeStatisticsAsync()
    {
        try
        {
            var bodyTypeGroups = _carRepository.GetAll(
                    selector: c => c.BodyType,
                    predicate: c => c.IsActive && c.BodyType.HasValue
                ).Where(bodyType => bodyType.HasValue)
                .GroupBy(bodyType => bodyType!.Value)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(kvp => kvp.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return await Task.FromResult(bodyTypeGroups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving body type statistics");
            throw;
        }
    }

    public async Task<Dictionary<int, int>> GetYearStatisticsAsync()
    {
        try
        {
            var yearGroups = _carRepository.GetAll(
                    selector: c => c.Year,
                    predicate: c => c.IsActive && c.Year.HasValue
                ).Where(year => year.HasValue)
                .GroupBy(year => year!.Value)
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(kvp => kvp.Key) // Order by year descending
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return await Task.FromResult(yearGroups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving year statistics");
            throw;
        }
    }


    private IQueryable<CarListing> ApplySorting(IQueryable<CarListing> query, string? sortBy, string? sortOrder)
    {
        var isDescending = sortOrder?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "price" => isDescending
                ? query.OrderByDescending(c => c.Price)
                : query.OrderBy(c => c.Price),
            "year" => isDescending
                ? query.OrderByDescending(c => c.Year)
                : query.OrderBy(c => c.Year),
            "mileage" => isDescending
                ? query.OrderByDescending(c => c.Mileage)
                : query.OrderBy(c => c.Mileage),
            "make" => isDescending
                ? query.OrderByDescending(c => c.Make)
                : query.OrderBy(c => c.Make),
            "model" => isDescending
                ? query.OrderByDescending(c => c.Model)
                : query.OrderBy(c => c.Model),
            "updated" => isDescending
                ? query.OrderByDescending(c => c.UpdatedAt)
                : query.OrderBy(c => c.UpdatedAt),
            _ => isDescending
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt)
        };
    }


    // Add these methods to your CarListingService class

    public async Task<bool> DeleteCarImageAsync(Guid imageId, string userId)
    {
        try
        {
            var image = _imageRepository.Get(
                selector: i => i,
                predicate: i => i.Id == imageId,
                include: query => query.Include(i => i.CarListing)
            );

            if (image == null || image.CarListing.ListingOwnerId != userId)
            {
                _logger.LogWarning("Unauthorized attempt to delete image {ImageId} by user {UserId}", imageId, userId);
                return false;
            }

            // Delete physical file
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(webRootPath, image.ImageUrl.TrimStart('/'));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // Delete from database
            _imageRepository.Delete(image);

            _logger.LogInformation("Car image {ImageId} deleted successfully", imageId);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting car image {ImageId}", imageId);
            throw;
        }
    }

    public async Task<List<CarImage>> GetCarImagesAsync(Guid carListingId)
    {
        try
        {
            var images = _imageRepository.GetAll(
                selector: i => i,
                predicate: i => i.CarListingId == carListingId,
                orderBy: query => query.OrderBy(i => i.DisplayOrder).ThenBy(i => i.Id)
            ).ToList();

            return await Task.FromResult(images);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving images for car listing {CarListingId}", carListingId);
            throw;
        }
    }

    public async Task<CarImage> CreateCarImageAsync(CarImage carImage)
    {
        try
        {
            carImage.Id = Guid.NewGuid();

            var result = _imageRepository.Insert(carImage);

            _logger.LogInformation("Car image created successfully with ID {ImageId} for car {CarListingId}",
                result.Id, carImage.CarListingId);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating car image for car listing {CarListingId}", carImage.CarListingId);
            throw;
        }
    }

    public async Task<bool> UpdateImageDisplayOrderAsync(Guid imageId, int displayOrder, string userId)
    {
        try
        {
            var image = _imageRepository.Get(
                selector: i => i,
                predicate: i => i.Id == imageId,
                include: query => query.Include(i => i.CarListing)
            );

            if (image == null || image.CarListing.ListingOwnerId != userId)
            {
                return false;
            }

            image.DisplayOrder = displayOrder;
            _imageRepository.Update(image);

            _logger.LogInformation("Image {ImageId} display order updated to {DisplayOrder}", imageId, displayOrder);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating display order for image {ImageId}", imageId);
            throw;
        }
    }

    public async Task<bool> SetMainImageAsync(Guid imageId, string userId)
    {
        try
        {
            var image = _imageRepository.Get(
                selector: i => i,
                predicate: i => i.Id == imageId,
                include: query => query.Include(i => i.CarListing)
            );

            if (image == null || image.CarListing.ListingOwnerId != userId)
            {
                return false;
            }

            // First, set all images of this car to not main
            var allImages = _imageRepository.GetAll(
                selector: i => i,
                predicate: i => i.CarListingId == image.CarListingId
            );

            foreach (var img in allImages)
            {
                img.IsPrimary = false;
                _imageRepository.Update(img);
            }

            // Then set the selected image as main
            image.IsPrimary = true;
            _imageRepository.Update(image);

            _logger.LogInformation("Image {ImageId} set as main image for car {CarListingId}",
                imageId, image.CarListingId);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting main image {ImageId}", imageId);
            throw;
        }
    }

    public async Task<CarImage?> GetMainImageAsync(Guid carListingId)
    {
        try
        {
            var mainImage = _imageRepository.Get(
                selector: i => i,
                predicate: i => i.CarListingId == carListingId && i.IsPrimary
            );

            // If no main image is set, return the first image
            if (mainImage == null)
            {
                mainImage = _imageRepository.GetAll(
                    selector: i => i,
                    predicate: i => i.CarListingId == carListingId,
                    orderBy: query => query.OrderBy(i => i.DisplayOrder) ).FirstOrDefault();
            }

            return await Task.FromResult(mainImage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting main image for car listing {CarListingId}", carListingId);
            throw;
        }
    }

    public async Task<int> GetImageCountAsync(Guid carListingId)
    {
        try
        {
            var count = _imageRepository.GetAll(
                selector: i => i.Id,
                predicate: i => i.CarListingId == carListingId
            ).Count();

            return await Task.FromResult(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image count for car listing {CarListingId}", carListingId);
            throw;
        }
    }

    public async Task<bool> ValidateImageOwnershipAsync(Guid imageId, string userId)
    {
        try
        {
            var image = _imageRepository.Get(
                selector: i => i,
                predicate: i => i.Id == imageId,
                include: query => query.Include(i => i.CarListing)
            );

            return await Task.FromResult(image?.CarListing.ListingOwnerId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating image ownership for image {ImageId} and user {UserId}",
                imageId, userId);
            throw;
        }
    }
}