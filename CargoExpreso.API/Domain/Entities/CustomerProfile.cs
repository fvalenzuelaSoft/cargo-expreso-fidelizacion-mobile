using CargoExpreso.API.Domain;

namespace CargoExpreso.API.Domain.Entities;

public class CustomerProfile
{
    public Guid                  Id                     { get; set; }
    public Guid                  CustomerId             { get; set; }
    public string?               Email                  { get; set; }
    public DateOnly?             BirthDate              { get; set; }
    public string?               Address                { get; set; }
    public byte?                 CountryId              { get; set; }
    public ProfileCompletionLevel ProfileCompletionLevel { get; set; } = ProfileCompletionLevel.PhoneOnly;
    public bool                  IsProfileComplete      { get; set; }
    public decimal               BonusApplied           { get; set; }
    public DateTime              UpdatedAt              { get; set; }

    public Customer  Customer { get; set; } = default!;
    public Country?  Country  { get; set; }
}
