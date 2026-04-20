using System.ComponentModel.DataAnnotations;

public class MenuItem
{
    [Key]
    public Guid MenuItemGuid { get; set; }

    [Required]
    public Guid OrderGuid { get; set; }

    [Required]
    public Order Order { get; set; } = null!;

    [Required]
    public required string Name { get; set; }

    [Required]
    public required string MenuType { get; set; }

    [Required]
    public required string Description { get; set; }

    [Required]
    public float Price { get; set; }
}