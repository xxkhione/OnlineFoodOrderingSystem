using System;
using System.ComponentModel.DataAnnotations;

public class OrderDTO
{
    public Guid BasketGuid { get; set; }
    public Boolean ReadBasket { get; set; }
    public List<MenuItemDTO>? MenuItems { get; set; }
}