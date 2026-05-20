namespace CargoExpreso.App;

public static class Constants
{
    // Android emulator → localhost: use 10.0.2.2
    // iOS simulator → localhost: use localhost
    // Physical device → use your machine's IP
    public const string ApiBaseUrl = "http://10.0.2.2:5001/";

    public static class Routes
    {
        public const string Login          = "api/v1/auth/login";
        public const string Register       = "api/v1/auth/register";
        public const string Refresh        = "api/v1/auth/refresh";
        public const string Logout         = "api/v1/auth/logout";
        public const string CustomerMe     = "api/v1/customers/me";
        public const string PointsBalance  = "api/v1/points/balance";
        public const string PointsHistory  = "api/v1/points/history";
        public const string ShipmentScan   = "api/v1/shipments/scan";
        public const string Redemptions    = "api/v1/redemptions";
    }
}
