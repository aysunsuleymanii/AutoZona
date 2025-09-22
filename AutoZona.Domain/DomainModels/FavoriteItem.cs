namespace AutoZona.Domain.DomainModels;

public class FavoriteItem : BaseEntity
{
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public Guid CarListingId { get; set; }
    public Guid FavoriteListId { get; set; }

    public virtual CarListing? CarListing { get; set; }
    public virtual FavoriteList? FavoriteList { get; set; }
}