namespace CargoExpreso.API.DTOs.Customers;

public class RegisterCustomerRequest
{
    public string  IdentityNumber { get; set; } = default!;
    public string  FirstName      { get; set; } = default!;
    public string  LastName       { get; set; } = default!;
    public string  Phone          { get; set; } = default!;
    public string? Email          { get; set; }
    public string? Address        { get; set; }
    public DateOnly? BirthDate    { get; set; }
    public byte?   CountryId      { get; set; }
    public string? DeviceToken    { get; set; }
}

public class UpdateProfileRequest
{
    public string? Email     { get; set; }
    public string? Address   { get; set; }
    public DateOnly? BirthDate { get; set; }
    public byte?   CountryId { get; set; }
    public string? DeviceToken { get; set; }
}

public class CustomerResponse
{
    public Guid    Id             { get; set; }
    public string  IdentityNumber { get; set; } = default!;
    public string  FullName       { get; set; } = default!;
    public string  Phone          { get; set; } = default!;
    public string? Email          { get; set; }
    public string  Status         { get; set; } = default!;
    public decimal Balance        { get; set; }
    public decimal TotalAccumulated { get; set; }
    public decimal TotalRedeemed    { get; set; }
    public string  ProfileLevel   { get; set; } = default!;
    public bool    IsProfileComplete { get; set; }
    public DateTime CreatedAt     { get; set; }
}
