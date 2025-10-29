namespace CineReserv.Models
{
    public class StripeSettings
    {
        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Currency { get; set; } = "usd"; // default currency used for Stripe charges
    }
}

