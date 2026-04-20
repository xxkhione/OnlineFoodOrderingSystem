using System.ComponentModel.DataAnnotations;

public class Order
{
    [Key]
    public Guid OrderGuid { get; set; }

    [Required]
    public Guid CustomerGuid { get; set; } 

    [Required]
    public Guid BasketGuid { get; set; }
    
    [Required]
    public DateTime CreatedDate { get; set; }

    [Required]
    public List<MenuItem> MenuItems { get; set; } = new();

}