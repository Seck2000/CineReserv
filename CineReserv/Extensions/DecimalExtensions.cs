using System.Globalization;

namespace CineReserv.Extensions
{
    public static class DecimalExtensions
    {
        /// <summary>
        /// Formate un montant en dollars américains (USD) avec le symbole $.
        /// </summary>
        public static string ToUsdString(this decimal value)
        {
            var usCulture = new CultureInfo("en-US");
            return value.ToString("C", usCulture);
        }
    }
}

