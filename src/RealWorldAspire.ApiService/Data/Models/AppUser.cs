using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RealWorldAspire.ApiService.Data.Models;

public class AppUser : IdentityUser
{
    [Required]
    public override string UserName 
    { 
        get => base.UserName!; 
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        set => base.UserName = value; 
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    }

    [Required]
    [EmailAddress]
    public override string Email 
    { 
        get => base.Email!; 
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        set => base.Email = value; 
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
    }
    
    [MaxLength(ValidationConstants.AppUser.BioMaxLength)]
    public string? Bio { get; set; }

    [MaxLength(ValidationConstants.AppUser.ImageMaxLength)]
    public string? Image { get; set; }
}