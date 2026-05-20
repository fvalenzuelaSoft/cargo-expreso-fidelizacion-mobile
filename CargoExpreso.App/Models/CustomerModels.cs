namespace CargoExpreso.App.Models;

public class CustomerProfile
{
    public Guid     Id                     { get; set; }
    public string   IdentityNumber         { get; set; } = "";
    public string   FirstName              { get; set; } = "";
    public string   LastName               { get; set; } = "";
    public string   FullName               => $"{FirstName} {LastName}";
    public string   Phone                  { get; set; } = "";
    public string?  Email                  { get; set; }
    public DateOnly? BirthDate             { get; set; }
    public string   Status                 { get; set; } = "";
    public DateTime RegisteredAt           { get; set; }
    public decimal  PointsBalance          { get; set; }
    public string   ProfileCompletionLevel { get; set; } = "";
}
