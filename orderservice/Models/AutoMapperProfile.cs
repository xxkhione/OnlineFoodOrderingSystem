using AutoMapper;
public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<CustomerDTO, Customer>();        
        CreateMap<OrderDTO, Order>();
        CreateMap<MenuItemDTO, MenuItem>();
    }
}