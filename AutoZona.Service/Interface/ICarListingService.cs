using AutoZona.Domain.DomainModels;
using AutoZona.Domain.Enums;

namespace AutoZona.Service.Interface;

public interface ICarListingService
{
    Task<CarListing> CreateCarListingAsync(CarListing carListing);
    Task<CarListing?> GetCarListingByIdAsync(Guid id);
    Task<CarListing> UpdateCarListingAsync(CarListing carListing);
    Task<bool> DeleteCarListingAsync(Guid id);

    Task<IEnumerable<CarListing>> GetAllActiveCarListingsAsync();

    Task<IEnumerable<CarListing>> SearchCarsAsync(
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
        string? city = null);

    Task<IEnumerable<CarListing>> GetUserCarListingsAsync(string userId);
    Task<bool> IsOwnerOfCarAsync(Guid carId, string userId);

    Task<IEnumerable<string>> GetAvailableMakesAsync();
    Task<IEnumerable<string>> GetAvailableModelsAsync(string make);
    Task<int> GetTotalActiveListingsCountAsync();

    public Task<(IEnumerable<CarListing> cars, int totalCount)> GetCarsWithPaginationAsync(
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
        string? sortOrder = "desc");
}