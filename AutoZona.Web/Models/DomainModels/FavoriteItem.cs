namespace AutoZona.Web.Models.DomainModels;

public class FavoriteItem
{
    public Guid Id { get; set; }
    public DateTime AddedAt { get; set; }
    
    public Guid CarListingId { get; set; } 
    public Guid FavoriteListId { get; set; } 
    
    public virtual CarListing? CarListing { get; set; } 
    public virtual FavoriteList? FavoriteList { get; set; }
}