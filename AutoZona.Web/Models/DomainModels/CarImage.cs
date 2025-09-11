namespace AutoZona.Web.Models.DomainModels;

public class CarImage
{
    public Guid Id { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public bool IsPrimary { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    public Guid? CarListingId { get; set; }
    
    public virtual CarListing? CarListing { get; set; } 

}