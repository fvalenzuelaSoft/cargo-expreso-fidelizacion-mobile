namespace CargoExpreso.API.Domain;

public enum CustomerStatus : byte { Active = 1, Blocked = 2, Suspended = 3 }
public enum UserRole      : byte { Admin = 1, Supervisor = 2, Cashier = 3 }
public enum UserStatus    : byte { Active = 1, Inactive = 2, Locked = 3 }
public enum ShipmentStatus : byte { Pending = 1, Scanned = 2, Expired = 3 }
public enum RedemptionRequestStatus : byte { Pending = 1, QrGenerated = 2, Applied = 3, Expired = 4, Cancelled = 5 }
public enum TransactionType : byte { Accumulation = 1, Redemption = 2, RegistrationBonus = 3, Expiration = 4, ManualAdjustment = 5 }
public enum AuditResult   : byte { Success = 1, Rejected = 2, Error = 3 }
public enum FraudAlertSeverity : byte { Low = 1, Medium = 2, High = 3, Critical = 4 }
public enum FraudAlertStatus  : byte { Open = 1, UnderReview = 2, Resolved = 3, Dismissed = 4 }
public enum ProfileCompletionLevel : byte { PhoneOnly = 1, WithEmail = 2, Complete = 3 }
public enum UserType : byte { Customer = 1, Operator = 2 }
