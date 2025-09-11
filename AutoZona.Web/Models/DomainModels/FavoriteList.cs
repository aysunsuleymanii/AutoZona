using AutoZona.Web.Models.IdentityModels;

namespace AutoZona.Web.Models.DomainModels;

public class FavoriteList
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid? ListOwnerId { get; set; }
    
    
    public AutoZonaApplicationUser? ListOwner { get; set; }
    public virtual ICollection<FavoriteItem> FavoriteItems { get; set; } = new List<FavoriteItem>();
    
}