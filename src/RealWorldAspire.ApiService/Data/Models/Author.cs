using System.ComponentModel.DataAnnotations;

namespace RealWorldAspire.ApiService.Data.Models;

public class Author
{
    public int AuthorId { get; set; }
    
    [MaxLength(50)]
    public string Username { get; set; }

    [MaxLength(500)]
    public string Bio { get; set; }
    
    [MaxLength(1000)]
    public string Image { get; set; }
    
    public bool Following { get; set; }

}
