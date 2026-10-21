namespace WebApplication1.Models;
using System.ComponentModel.DataAnnotations;

public class Owner
{
    public int OwnerID { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [RegularExpression(@"^[0-9]{8,15}$")]
    public string Phone { get; set; }

    public ICollection<Pet>? Pets { get; set; }
}