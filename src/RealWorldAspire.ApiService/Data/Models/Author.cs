using System.ComponentModel.DataAnnotations;

namespace RealWorldAspire.ApiService.Data.Models;

public class Author
{
    public int AuthorId { get; set; }
    
    [MaxLength(ValidationConstants.Author.UserNameMaxLength)]
    public string Username { get; set; }

    [MaxLength(ValidationConstants.Author.BioMaxLength)]
    public string Bio { get; set; }
    
    [MaxLength(ValidationConstants.Author.ImageMaxLength)]
    public string Image { get; set; }
    
    public bool Following { get; set; }

}
