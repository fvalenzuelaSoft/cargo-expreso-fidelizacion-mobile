using CargoExpreso.API.Domain;

namespace CargoExpreso.API.Domain.Entities;

public class User
{
    public Guid     Id                   { get; set; }
    public Guid?    BranchId             { get; set; }
    public string   Username             { get; set; } = default!;
    public string   PasswordHash         { get; set; } = default!;
    public string   FullName             { get; set; } = default!;
    public string?  Email                { get; set; }
    public UserRole Role                 { get; set; }
    public UserStatus Status             { get; set; } = UserStatus.Active;
    public int      FailedLoginAttempts  { get; set; }
    public DateTime? LockedUntil         { get; set; }
    public DateTime? LastLoginAt         { get; set; }
    public byte[]   RowVersion           { get; set; } = default!;
    public DateTime CreatedAt            { get; set; }
    public DateTime UpdatedAt            { get; set; }
    public string   CreatedBy            { get; set; } = "SYSTEM";

    public Branch?  Branch               { get; set; }
}
