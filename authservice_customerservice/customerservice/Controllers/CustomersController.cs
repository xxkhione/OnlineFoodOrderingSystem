using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(
    ILogger<CustomersController> logger,
    CustomerServiceDbContext db,
    IMapper mapper) : ControllerBase
{
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { Success = true, Message = "CustomerService is up and running!" });
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var customers = await db.Customers.ToListAsync();
            return Ok(new { Success = true, Message = "All customers returned.", Customers = customers });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving customers.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("{customerGuid:guid}")]
    public async Task<IActionResult> GetById(Guid customerGuid)
    {
        try
        {
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerGuid == customerGuid);
            if (customer == null)
                return NotFound(new { Success = false, Message = "Customer not found." });

            return Ok(new { Success = true, Message = "Customer fetched.", Customer = customer });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching customer {CustomerGuid}.", customerGuid);
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerDTO customerDTO)
    {
        try
        {
            var existingCustomer = await db.Customers.FirstOrDefaultAsync(c => c.Email == customerDTO.Email);
            if (existingCustomer is not null)
                return Ok(new
                {
                    Success = true,
                    Message = "Customer already exists.",
                    CustomerGuid = existingCustomer.CustomerGuid,
                    Email = existingCustomer.Email
                });

            var customer = mapper.Map<Customer>(customerDTO);
            customer.CustomerGuid = Guid.NewGuid();
            customer.CreatedDate = DateTime.UtcNow;
            customer.PasswordHash = HashPassword(customerDTO.Password);

            await db.Customers.AddAsync(customer);
            await db.SaveChangesAsync();

            logger.LogInformation("Customer created with GUID {CustomerGuid}.", customer.CustomerGuid);
            return Ok(new { Success = true, Message = "Customer created.", CustomerGuid = customer.CustomerGuid });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating customer.");
            return StatusCode(500, "Internal server error.");
        }
    }

    // Internal endpoint called by AuthService to verify credentials and return safe customer info.
    // Returns CustomerGuid, Username, and Email — never exposes PasswordHash over the wire.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] CustomerDTO customerDTO)
    {
        try
        {
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.Email == customerDTO.Email);
            if (customer == null)
            {
                logger.LogWarning("Login attempt failed — email not found: {Email}.", customerDTO.Email);
                return Unauthorized();
            }

            if (!VerifyPassword(customer.PasswordHash, customerDTO.Password))
            {
                logger.LogWarning("Login attempt failed — invalid password for {Email}.", customerDTO.Email);
                return Unauthorized();
            }

            logger.LogInformation("Login successful for {Email}.", customerDTO.Email);
            return Ok(new { customer.CustomerGuid, customer.Username, customer.Email });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for {Email}.", customerDTO.Email);
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPut("{customerGuid:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid customerGuid, [FromBody] CustomerDTO customerDTO)
    {
        try
        {
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerGuid == customerGuid);
            if (customer is null)
                return NotFound(new { Success = false, Message = "Customer not found." });

            var existingUsername = customer.Username;
            mapper.Map(customerDTO, customer);
            customer.Username ??= existingUsername;
            await db.SaveChangesAsync();

            logger.LogInformation("Customer {CustomerGuid} updated.", customerGuid);
            return Ok(new { Success = true, Message = "Customer updated.", CustomerGuid = customer.CustomerGuid });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating customer {CustomerGuid}.", customerGuid);
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpDelete("{customerGuid:guid}")]
    public async Task<IActionResult> Delete(Guid customerGuid)
    {
        try
        {
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.CustomerGuid == customerGuid);
            if (customer is null)
                return NotFound(new { Success = false, Message = "Customer not found." });

            db.Customers.Remove(customer);
            await db.SaveChangesAsync();

            logger.LogInformation("Customer {CustomerGuid} deleted.", customerGuid);
            return Ok(new { Success = true, Message = "Customer deleted.", CustomerGuid = customer.CustomerGuid });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting customer {CustomerGuid}.", customerGuid);
            return StatusCode(500, "Internal server error.");
        }
    }

    private static string HashPassword(string password)
    {
        var passwordHasher = new PasswordHasher<object>();
        return passwordHasher.HashPassword(null!, password);
    }

    private static bool VerifyPassword(string hashedPassword, string enteredPassword)
    {
        var passwordHasher = new PasswordHasher<object>();
        var result = passwordHasher.VerifyHashedPassword(null!, hashedPassword, enteredPassword);
        return result == PasswordVerificationResult.Success;
    }
}
