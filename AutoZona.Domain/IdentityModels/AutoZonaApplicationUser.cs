using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.JavaScript;
using AutoZona.Domain.DomainModels;
using AutoZona.Domain.Enums;


namespace AutoZona.Domain.IdentityModels;

using Microsoft.AspNetCore.Identity;

public class AutoZonaApplicationUser : IdentityUser
{
    [Required] public string? FirstName { get; set; }
    [Required] public string? LastName { get; set; }
    [Required] public string? City { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Role role { get; set; } = Role.User;
    public virtual ICollection<CarListing> CarListings { get; set; } = new List<CarListing>();
    public virtual ICollection<FavoriteList> FavoritesLists { get; set; } = new List<FavoriteList>();
}