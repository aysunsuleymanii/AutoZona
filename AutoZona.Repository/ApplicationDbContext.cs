using AutoZona.Domain.DomainModels;
using AutoZona.Domain.IdentityModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutoZona.Repository;

public class ApplicationDbContext : IdentityDbContext<AutoZonaApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CarListing> CarListings { get; set; }
    public virtual DbSet<CarImage> CarImages { get; set; }
    public virtual DbSet<FavoriteList> FavoriteLists { get; set; }
    public virtual DbSet<FavoriteItem> FavoriteItems { get; set; }
}