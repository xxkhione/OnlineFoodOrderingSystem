public class MenuItemDTO
{
    public Guid MenuItemGuid { get; set; }
    public required string Name { get; set; }
    public required string MenuType { get; set; }
    public required string Description { get; set; }
    public float Price { get; set; }
}