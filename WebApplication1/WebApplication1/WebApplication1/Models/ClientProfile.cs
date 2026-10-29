using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

// Свързва профил за вход с данните на собственика без да смесва двете таблици.
public class ClientProfile
{
    [Key]
    public string UserId { get; set; } = string.Empty;

    public int OwnerId { get; set; }

    public ApplicationUser? User { get; set; }
    public Owner? Owner { get; set; }
}
