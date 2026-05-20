namespace CargoExpreso.App.Models;

public class LoginRequest
{
    public string IdentityNumber { get; set; } = "";
    public string Phone          { get; set; } = "";
}

public class RegisterCustomerRequest
{
    public string   IdentityNumber { get; set; } = "";
    public string   FirstName      { get; set; } = "";
    public string   LastName       { get; set; } = "";
    public string   Phone          { get; set; } = "";
    public string?  Email          { get; set; }
    public DateOnly? BirthDate     { get; set; }
}

public class LoginResponse
{
    public string          AccessToken      { get; set; } = "";
    public string          RefreshToken     { get; set; } = "";
    public int             ExpiresInMinutes { get; set; }
    public CustomerSummary Customer         { get; set; } = new();
}

public class CustomerSummary
{
    public Guid    Id             { get; set; }
    public string  FullName       { get; set; } = "";
    public string  IdentityNumber { get; set; } = "";
    public decimal PointsBalance  { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = "";
}
