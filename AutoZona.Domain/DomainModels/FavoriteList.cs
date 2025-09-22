using AutoZona.Domain.IdentityModels;

namespace AutoZona.Domain.DomainModels;

public class FavoriteList : BaseEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? ListOwnerId { get; set; }
    
    
    public virtual AutoZonaApplicationUser ListOwner { get; set; }
    public virtual ICollection<FavoriteItem> FavoriteItems { get; set; } = new List<FavoriteItem>();
    
}