using AutoZona.Domain.DomainModels;
using AutoZona.Repository;
using AutoZona.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoZona.Service.Implementation;

public class CarImageService : ICarImageService
{
    private readonly IRepository<CarImage> _imageRepository;
    private readonly IRepository<CarListing> _carRepository;
    private readonly ILogger<CarImageService> _logger;

    public CarImageService(IRepository<CarListing> carRepository, IRepository<CarImage> imageRepository,
        ILogger<CarImageService> logger)
    {
        _imageRepository = imageRepository;
        _carRepository = carRepository;
        _logger = logger;
    }


    public async Task<CarImage> AddImageToCarAsync(Guid carId, string imageUrl, string? description, string userId)
    {
        try
        {
            // Verify the car exists and belongs to the user
            var car = _carRepository.Get(
                selector: c => c,
                predicate: c => c.Id == carId && c.ListingOwnerId == userId && c.IsActive
            );

            if (car == null)
                throw new UnauthorizedAccessException("Car not found or access denied");

            // Get the current image count to set display order
            var imageCount = _imageRepository.GetAll(
                selector: img => img.Id,
                predicate: img => img.CarListingId == carId
            ).Count();

            // Create new image
            var carImage = new CarImage
            {
                Id = Guid.NewGuid(),
                CarListingId = carId,
                ImageUrl = imageUrl,
                Description = description,
                IsPrimary = imageCount == 0, // First image is automatically primary
                DisplayOrder = imageCount
            };

            return await Task.FromResult(_imageRepository.Insert(carImage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding image to car {CarId} for user {UserId}", carId, userId);
            throw;
        }
    }

    public async Task<IEnumerable<CarImage>> AddMultipleImagesToCarAsync(Guid carId,
        List<(string Url, string? Description)> images, string userId)
    {
        try
        {
            // Verify the car exists and belongs to the user
            var car = _carRepository.Get(
                selector: c => c,
                predicate: c => c.Id == carId && c.ListingOwnerId == userId && c.IsActive
            );

            if (car == null)
                throw new UnauthorizedAccessException("Car not found or access denied");

            // Get the current image count for display order
            var currentImageCount = _imageRepository.GetAll(
                selector: img => img.Id,
                predicate: img => img.CarListingId == carId
            ).Count();

            var carImages = new List<CarImage>();

            for (int i = 0; i < images.Count; i++)
            {
                var carImage = new CarImage
                {
                    Id = Guid.NewGuid(),
                    CarListingId = carId,
                    ImageUrl = images[i].Url,
                    Description = images[i].Description,
                    IsPrimary = currentImageCount == 0 && i == 0, // First image is primary if no existing images
                    DisplayOrder = currentImageCount + i
                };

                carImages.Add(_imageRepository.Insert(carImage));
            }

            return await Task.FromResult(carImages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding multiple images to car {CarId} for user {UserId}", carId, userId);
            throw;
        }
    }

    public async Task<CarImage?> GetImageByIdAsync(Guid imageId)
    {
        try
        {
            return await Task.FromResult(_imageRepository.Get(
                selector: img => img,
                predicate: img => img.Id == imageId,
                include: query => query.Include(img => img.CarListing)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving image {ImageId}", imageId);
            throw;
        }
    }

    public async Task<IEnumerable<CarImage>> GetCarImagesAsync(Guid carId)
    {
        try
        {
            return await Task.FromResult(_imageRepository.GetAll(
                selector: img => img,
                predicate: img => img.CarListingId == carId,
                orderBy: query => query.OrderBy(img => img.DisplayOrder)
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving images for car {CarId}", carId);
            throw;
        }
    }

    public async Task<CarImage> UpdateImageAsync(Guid imageId, string? description, string userId)
    {
        try
        {
            var image = _imageRepository.Get(
                selector: img => img,
                predicate: img => img.Id == imageId,
                include: query => query.Include(img => img.CarListing)
            );

            if (image == null || image.CarListing?.ListingOwnerId != userId)
                throw new UnauthorizedAccessException("Image not found or access denied");

            image.Description = description;

            return await Task.FromResult(_imageRepository.Update(image));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating image {ImageId} for user {UserId}", imageId, userId);
            throw;
        }
    }

    public async Task<bool> DeleteImageAsync(Guid imageId, string userId)
    {
        try
        {
            var image = _imageRepository.Get(
                selector: img => img,
                predicate: img => img.Id == imageId,
                include: query => query.Include(img => img.CarListing)
            );

            if (image == null || image.CarListing?.ListingOwnerId != userId)
                return false;

            var wasPrimary = image.IsPrimary;
            var carId = image.CarListingId;

            _imageRepository.Delete(image);

            // If the deleted image was primary, set another image as primary
            if (wasPrimary && carId.HasValue)
            {
                var nextImage = _imageRepository.Get(
                    selector: img => img,
                    predicate: img => img.CarListingId == carId.Value,
                    orderBy: query => query.OrderBy(img => img.DisplayOrder)
                );

                if (nextImage != null)
                {
                    nextImage.IsPrimary = true;
                    _imageRepository.Update(nextImage);
                }
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageId} for user {UserId}", imageId, userId);
            throw;
        }
    }

    public async Task<bool> SetPrimaryImageAsync(Guid imageId, string userId)
    {
        try
        {
            var image = _imageRepository.Get(
                selector: img => img,
                predicate: img => img.Id == imageId,
                include: query => query.Include(img => img.CarListing)
            );

            if (image == null || image.CarListing?.ListingOwnerId != userId)
                return false;

            // Remove primary status from all other images of this car
            var otherImages = _imageRepository.GetAll(
                selector: img => img,
                predicate: img => img.CarListingId == image.CarListingId && img.Id != imageId && img.IsPrimary
            );

            foreach (var otherImage in otherImages)
            {
                otherImage.IsPrimary = false;
                _imageRepository.Update(otherImage);
            }

            // Set this image as primary
            image.IsPrimary = true;
            _imageRepository.Update(image);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting primary image {ImageId} for user {UserId}", imageId, userId);
            throw;
        }
    }

    public async Task<bool> ReorderImagesAsync(Guid carId, List<Guid> imageIds, string userId)
    {
        try
        {
            // Verify the car belongs to the user
            var car = _carRepository.Get(
                selector: c => c,
                predicate: c => c.Id == carId && c.ListingOwnerId == userId && c.IsActive
            );

            if (car == null)
                return false;

            // Get all images for this car
            var images = _imageRepository.GetAll(
                selector: img => img,
                predicate: img => img.CarListingId == carId
            ).ToList();

            if (imageIds.Any(id => !images.Any(img => img.Id == id)))
                return false;

            for (int i = 0; i < imageIds.Count; i++)
            {
                var image = images.First(img => img.Id == imageIds[i]);
                image.DisplayOrder = i;
                _imageRepository.Update(image);
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering images for car {CarId} and user {UserId}", carId, userId);
            throw;
        }
    }

    public async Task<CarImage?> GetPrimaryImageAsync(Guid carId)
    {
        try
        {
            return await Task.FromResult(_imageRepository.Get(
                selector: img => img,
                predicate: img => img.CarListingId == carId && img.IsPrimary
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving primary image for car {CarId}", carId);
            throw;
        }
    }

    public async Task<bool> IsImageOwnerAsync(Guid imageId, string userId)
    {
        try
        {
            var image = _imageRepository.Get(
                selector: img => img.CarListing.ListingOwnerId,
                predicate: img => img.Id == imageId,
                include: query => query.Include(img => img.CarListing)
            );

            return await Task.FromResult(image == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking image ownership for image {ImageId} and user {UserId}", imageId,
                userId);
            throw;
        }
    }

    public async Task<int> GetImageCountForCarAsync(Guid carId)
    {
        try
        {
            var count = _imageRepository.GetAll(
                selector: img => img.Id,
                predicate: img => img.CarListingId == carId
            ).Count();

            return await Task.FromResult(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting image count for car {CarId}", carId);
            throw;
        }
    }

    public async Task<bool> ValidateImageUrlAsync(string imageUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return false;

            if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating image URL {ImageUrl}", imageUrl);
            return false;
        }
    }
}