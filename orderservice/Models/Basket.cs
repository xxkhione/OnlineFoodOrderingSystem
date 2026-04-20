using System.ComponentModel.DataAnnotations;

public class Basket
{
    [Key]
    public Guid BasketGuid { get; set; }

    [Required]
    public List<FoodItemDTO>? FoodItems { get; set; }
}