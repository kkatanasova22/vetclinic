namespace WebApplication1.Models;
using System.ComponentModel.DataAnnotations;

public class Pet
{
    public int PetID { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; set; }

    [Required]
    public string Species { get; set; }

    public string Breed { get; set; }

    [DataType(DataType.Date)]
    public DateTime BirthDate { get; set; }

    [Required]
    public int OwnerID { get; set; }

    public Owner Owner { get; set; }
}