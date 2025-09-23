using AutoZona.Domain.DomainModels;

namespace AutoZona.Service.Interface;

public interface IFavoriteItemService
{
    Task<FavoriteList> CreateFavoritesListAsync(string name, string? description, string userId);
    Task<FavoriteList?> GetFavoritesListByIdAsync(Guid listId, string userId);
    Task<IEnumerable<FavoriteList>> GetUserFavoritesListsAsync(string userId);
    Task<FavoriteList> UpdateFavoritesListAsync(Guid listId, string name, string? description, string userId);
    Task<bool> DeleteFavoritesListAsync(Guid listId, string userId);

    Task<bool> AddCarToFavoritesAsync(Guid listId, Guid carId, string userId);
    Task<bool> RemoveCarFromFavoritesAsync(Guid listId, Guid carId, string userId);
    Task<bool> IsCarInFavoritesAsync(Guid listId, Guid carId, string userId);
    Task<IEnumerable<FavoriteList>> GetFavoritesListsContainingCarAsync(Guid carId, string userId);

    Task<int> GetFavoritesCountAsync(Guid listId);
    Task<bool> IsOwnerOfFavoritesListAsync(Guid listId, string userId);
}