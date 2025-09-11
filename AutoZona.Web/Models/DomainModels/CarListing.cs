using AutoZona.Web.Models.Enums;
using AutoZona.Web.Models.IdentityModels;

namespace AutoZona.Web.Models.DomainModels;

public class CarListing
{
    public Guid Id { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public decimal? Price { get; set; }
    public string? Description { get; set; }
    public int? Mileage { get; set; }
    public FuelType? Fuel { get; set; }
    public Color? Color { get; set; }
    public Transmission? Transmission { get; set; }
    public BodyType? BodyType { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ListingOwnerId { get; set; }
    
    public AutoZonaApplicationUser? ListingOwner { get; set; }
    public virtual ICollection<CarImage> Images { get; set; } = new List<CarImage>();
    public virtual ICollection<FavoriteItem> FavoriteItems { get; set; } = new List<FavoriteItem>();
}