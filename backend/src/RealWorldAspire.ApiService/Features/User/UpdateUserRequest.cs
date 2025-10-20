using System.ComponentModel.DataAnnotations;
using RealWorldAspire.ApiService.Data.Models;

namespace RealWorldAspire.ApiService.Features.User;

public class UpdateUserRequest
{
    public required UserModel User { get; set; }
    
    public class UserModel
    {
        [MaxLength(ValidationConstants.AppUser.UserNameMaxLength)]
        public string? Username { get; set; }
        
        [MaxLength(ValidationConstants.AppUser.EmailMaxLength)]
        public required string Email { get; set; }

        public string? Password { get; set; }

        [MaxLength(ValidationConstants.AppUser.BioMaxLength)]
        public string? Bio { get; set; }
        
        [MaxLength(ValidationConstants.AppUser.ImageMaxLength)]
        public string? Image { get; set; }
    }
}