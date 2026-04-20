using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(
    ILogger<OrdersController> logger,
    OrderServiceDbContext db,
    IMapper mapper,
    IConfiguration config,
    IHttpClientFactory httpClient) : ControllerBase
{
    private readonly HttpClient _httpClient = httpClient.CreateClient();

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok("Hello from OrderController!");
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var orders = await db.Orders.ToListAsync();
            return Ok(new { Success = true, Message = "Orders retrieved.", Orders = orders });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving orders.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("with-menu-items")]
    public async Task<IActionResult> GetAllWithMenuItems()
    {
        try
        {
            var orders = await db.Orders.Include(o => o.MenuItems).ToListAsync();
            return Ok(new { Success = true, Message = "Orders with menu items retrieved.", Orders = orders });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving orders with menu items.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("{orderGuid:guid}")]
    public async Task<IActionResult> GetByOrderGuid(Guid orderGuid)
    {
        try
        {
            var order = await db.Orders.Include(o => o.MenuItems).FirstOrDefaultAsync(o => o.OrderGuid == orderGuid);
            if (order == null)
                return NotFound($"Order with GUID {orderGuid} not found.");

            return Ok(new { Success = true, Message = "Order retrieved.", Order = order });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order {OrderGuid}.", orderGuid);
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("user/{customerGuid:guid}")]
    public async Task<IActionResult> GetByCustomerGuid(Guid customerGuid)
    {
        try
        {
            var orders = await db.Orders.Include(o => o.MenuItems).Where(o => o.CustomerGuid == customerGuid).ToListAsync();
            return Ok(new { Success = true, Message = "Orders by customer retrieved.", Orders = orders });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving orders for customer {CustomerGuid}.", customerGuid);
            return StatusCode(500, "Internal server error.");
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OrderDTO orderDTO)
    {
        try
        {
            var userGuidClaim = User.Claims.FirstOrDefault(c => c.Type == "UserGuid")?.Value;
            if (string.IsNullOrEmpty(userGuidClaim))
            {
                return Unauthorized();
            }
            Guid userguid = Guid.Parse(userGuidClaim);

            var username = User.Identity?.Name; // from ClaimTypes.Name
            var useremail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var order = mapper.Map<Order>(orderDTO);
            order.CustomerGuid = userguid;
            order.OrderGuid = Guid.NewGuid();
            order.CreatedDate = DateTime.UtcNow;


            db.Orders.Add(order);
            await db.SaveChangesAsync();

            var notification = new OrderNotification
            {
                CustomerGuid = order.CustomerGuid,
                OrderGuid = order.OrderGuid,
                Name = username,
                Email = useremail,
                Message = $"Your AMAZING order {order.OrderGuid} was created successfully."
            };

            var messageServiceUrl = config["MessageServiceUrl"];
            if (!string.IsNullOrEmpty(messageServiceUrl))
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync(messageServiceUrl, notification);
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send notification to MessageService.");
                }
            }
            else
            {
                logger.LogWarning("MessageService URL not configured — skipping notification.");
            }

            return CreatedAtAction(nameof(GetByOrderGuid), new { orderGuid = order.OrderGuid }, new
            {
                Success = true,
                Message = "Order created.",
                OrderGuid = order.OrderGuid,
                CustomerGuid = order.CustomerGuid
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating order.");
            return StatusCode(500, "Internal server error.");
        }
    }

        
    [HttpDelete("{orderGuid:guid}")]
    public async Task<IActionResult> Delete(Guid orderGuid)
    {
        try
        {
            var order = await db.Orders.Include(o => o.MenuItems).FirstOrDefaultAsync(o => o.OrderGuid == orderGuid);
            if (order == null)
                return NotFound($"Order with GUID {orderGuid} not found.");

            db.Orders.Remove(order);
            await db.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Order and associated menu items deleted." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting order {OrderGuid}.", orderGuid);
            return StatusCode(500, "Internal server error.");
        }
    }
}
