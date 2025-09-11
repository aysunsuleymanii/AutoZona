using AutoZona.Web.Models.IdentityModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutoZona.Web.Data;

public class ApplicationDbContext : IdentityDbContext<AutoZonaApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}