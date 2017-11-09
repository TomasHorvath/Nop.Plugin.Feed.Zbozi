using System.Collections.Generic;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Feed.Zbozi.Models
{
    public class ZboziFeedModel
    {
        public ZboziFeedModel()
        {
            AvailableStores = new List<SelectListItem>();
            AvailableCurrencies = new List<SelectListItem>();
            AvailableGoogleCategories = new List<SelectListItem>();
            GeneratedFiles = new List<GeneratedFileModel>();
        }

        [NopResourceDisplayName("Plugins.Feed.Zbozi.ProductPictureSize")]
        public int ProductPictureSize { get; set; }


        [NopResourceDisplayName("Plugins.Feed.Zbozi.Store")]
        public int StoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }

        [NopResourceDisplayName("Plugins.Feed.Zbozi.Currency")]
        public int CurrencyId { get; set; }
        public IList<SelectListItem> AvailableCurrencies { get; set; }

        [NopResourceDisplayName("Plugins.Feed.Zbozi.DefaultGoogleCategory")]
        public string DefaultGoogleCategory { get; set; }
        public IList<SelectListItem> AvailableGoogleCategories { get; set; }

        [NopResourceDisplayName("Plugins.Feed.Zbozi.PassShippingInfoWeight")]
        public bool PassShippingInfoWeight { get; set; }

        [NopResourceDisplayName("Plugins.Feed.Zbozi.PassShippingInfo")]
        public bool PassShippingInfo { get; set; }

        [NopResourceDisplayName("Plugins.Feed.Zbozi.PricesConsiderPromotions")]
        public bool PricesConsiderPromotions { get; set; }

        [NopResourceDisplayName("Plugins.Feed.Zbozi.StaticFilePath")]
        public IList<GeneratedFileModel> GeneratedFiles { get; set; }




		[NopResourceDisplayName("Plugins.Feed.Zbozi.DefaultDeliveryDate")]
		public string DefaultDeliveryDate { get; set; }



		public class GeneratedFileModel : BaseNopModel
        {
            public string StoreName { get; set; }
            public string FileUrl { get; set; }
        }

        public class GoogleProductModel : BaseNopModel
        {

			
			[NopResourceDisplayName("Plugins.Feed.Zbozi.MAXCPC_SEARCH")]
			public string MAXCPC_SEARCH { get; set; }

			[NopResourceDisplayName("Plugins.Feed.Zbozi.MAXCPC")]
			public string MAXCPC { get; set; }

			[NopResourceDisplayName("Plugins.Feed.Zbozi.EXTRAMESSAGE")]
			public string EXTRAMESSAGE { get; set; }

			[NopResourceDisplayName("Plugins.Feed.Zbozi.ZboziProductName")]
			public string ZboziProductName { get; set; }



			public int ProductId { get; set; }

            [NopResourceDisplayName("Plugins.Feed.Zbozi.Products.ProductName")]
            public string ProductName { get; set; }

            [NopResourceDisplayName("Plugins.Feed.Zbozi.Products.ZboziCategory")]
            public string ZboziCategory { get; set; }

            [NopResourceDisplayName("Plugins.Feed.Zbozi.Products.Gender")]
            public string Gender { get; set; }

            [NopResourceDisplayName("Plugins.Feed.Zbozi.Products.AgeGroup")]
            public string AgeGroup { get; set; }

            [NopResourceDisplayName("Plugins.Feed.Zbozi.Products.Color")]
            public string Color { get; set; }

            [NopResourceDisplayName("Plugins.Feed.Zbozi.Products.Size")]
            public string GoogleSize { get; set; }

            [NopResourceDisplayName("Plugins.Feed.Zbozi.Products.CustomGoods")]
            public bool CustomGoods { get; set; }
        }
    }
}