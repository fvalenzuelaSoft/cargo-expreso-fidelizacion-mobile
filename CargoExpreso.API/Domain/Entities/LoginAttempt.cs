using CargoExpreso.API.Domain;

namespace CargoExpreso.API.Domain.Entities;

public class LoginAttempt
{
    public long     Id             { get; set; }
    public string?  IdentityNumber { get; set; }
    public DateTime AttemptedAt   { get; set; }
    public bool     IsSuccessful  { get; set; }
    public string?  IpAddress     { get; set; }
    public string?  DeviceInfo    { get; set; }
    public string?  FailureReason { get; set; }
    public UserType UserType      { get; set; } = UserType.Customer;
}
