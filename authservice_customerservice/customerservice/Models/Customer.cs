using System.ComponentModel.DataAnnotations;
public class Customer
{
    [Key]
    public Guid CustomerGuid { get; set; }

    [Required]
    public String Username { get; set; }

    [Required]
    public String Email { get; set; }

    [Required]
    public String Password { get; set; }

    [Required]
    public String PasswordHash { get; set; }

    [Required]
    public DateTime CreatedDate { get; set; }

    string toString()
    {
        return this.CustomerGuid.ToString();
    }
}