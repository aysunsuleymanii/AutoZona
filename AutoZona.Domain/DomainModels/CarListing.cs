using System.ComponentModel.DataAnnotations;
using AutoZona.Domain.Enums;
using AutoZona.Domain.IdentityModels;


namespace AutoZona.Domain.DomainModels;

public class CarListing : BaseEntity
{
    [Required] public string? Make { get; set; }
    [Required] public string? Model { get; set; }
    [Required] public int? Year { get; set; }
    [Required] public decimal? Price { get; set; }
    public string? Description { get; set; }
    [Required] public int? Mileage { get; set; }
    [Required] public FuelType? Fuel { get; set; }

    public Color? Color { get; set; }
    public Transmission? Transmission { get; set; }
    public BodyType? BodyType { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ListingOwnerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AutoZonaApplicationUser? ListingOwner { get; set; }
    public virtual ICollection<CarImage> Images { get; set; } = new List<CarImage>();
    public virtual ICollection<FavoriteItem> FavoriteItems { get; set; } = new List<FavoriteItem>();
}