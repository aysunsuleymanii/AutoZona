using System.Runtime.InteropServices.JavaScript;
using AutoZona.Web.Models.Enums;

namespace AutoZona.Web.Models.IdentityModels;
using Microsoft.AspNetCore.Identity;

public class AutoZonaApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public DateTime? CreatedAt { get; set; }
    public Role role = Role.User;
}