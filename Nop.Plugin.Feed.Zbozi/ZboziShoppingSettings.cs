using Nop.Core.Configuration;

namespace Nop.Plugin.Feed.Zbozi
{
    public class ZboziShoppingSettings : ISettings
    {
        /// <summary>
        /// Product picture size
        /// </summary>
        public int ProductPictureSize { get; set; }

        /// <summary>
        /// A value indicating whether we should pass shipping info (weight)
        /// </summary>
        public bool PassShippingInfoWeight { get; set; }

        /// <summary>
        /// A value indicating whether we should pass shipping info (dimensions)
        /// </summary>
        public bool PassShippingInfo { get; set; }


		/// <summary>
		/// A value indicating whether we should calculate prices considering promotions (tier prices, discounts, special prices, etc)
		/// </summary>
		public bool PricesConsiderPromotions { get; set; }

        /// <summary>
        /// Store identifier for which feed file(s) will be generated
        /// </summary>
        public int StoreId { get; set; }
        /// <summary>
        /// Currency identifier for which feed file(s) will be generated
        /// </summary>
        public int CurrencyId { get; set; }

        /// <summary>
        /// Default Google category
        /// </summary>
        public string DefaultGoogleCategory { get; set; }

		/// <summary>
		/// Default Delivery Date
		/// </summary>
		public string DefaultDeliveryDate { get; set; } = "2";
       
        /// <summary>
        /// Static file name of the feed
        /// </summary>
        public string StaticFileName { get; set; }

        /// <summary>
        /// Number of days for expiration date
        /// </summary>
        public int ExpirationNumberOfDays { get; set; }



		


	}
}