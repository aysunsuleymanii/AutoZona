using AutoZona.Domain.DomainModels;

namespace AutoZona.Service.Interface;

public interface ICarImageService
{
    Task<CarImage> AddImageToCarAsync(Guid carId, string imageUrl, string? description, string userId);

    Task<IEnumerable<CarImage>> AddMultipleImagesToCarAsync(Guid carId, List<(string Url, string? Description)> images,
        string userId);

    Task<CarImage?> GetImageByIdAsync(Guid imageId);
    Task<IEnumerable<CarImage>> GetCarImagesAsync(Guid carId);
    Task<CarImage> UpdateImageAsync(Guid imageId, string? description, string userId);
    Task<bool> DeleteImageAsync(Guid imageId, string userId);

    Task<bool> SetPrimaryImageAsync(Guid imageId, string userId);
    Task<bool> ReorderImagesAsync(Guid carId, List<Guid> imageIds, string userId);
    Task<CarImage?> GetPrimaryImageAsync(Guid carId);

    Task<bool> IsImageOwnerAsync(Guid imageId, string userId);
    Task<int> GetImageCountForCarAsync(Guid carId);
    Task<bool> ValidateImageUrlAsync(string imageUrl);
}