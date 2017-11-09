using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Routing;
using System.Xml;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Stores;
using Nop.Core.Plugins;
using Nop.Plugin.Feed.Zbozi.Data;
using Nop.Plugin.Feed.Zbozi.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Seo;
using Nop.Services.Tax;
using Nop.Services.Shipping.Date;
using Nop.Services.Shipping;
using Nop.Plugin.Shipping.FixedOrByWeight.Services;

namespace Nop.Plugin.Feed.Zbozi
{
	public class ZboziShoppingService : BasePlugin, IMiscPlugin
	{
		#region Fields

		private readonly IZboziService _zboziService;
		private readonly IPriceCalculationService _priceCalculationService;
		private readonly ITaxService _taxService;
		private readonly IProductService _productService;
		private readonly ICategoryService _categoryService;
		private readonly IManufacturerService _manufacturerService;
		private readonly IPictureService _pictureService;
		private readonly ICurrencyService _currencyService;
		private readonly ILanguageService _languageService;
		private readonly ISettingService _settingService;
		private readonly IWorkContext _workContext;
		private readonly IMeasureService _measureService;
		private readonly IDateRangeService _dateRangeService;
		private readonly Nop.Services.Shipping.IShippingService _shippingService;
		private readonly IShippingByWeightService _shippingByWeightService;
		private readonly MeasureSettings _measureSettings;
		private readonly ZboziShoppingSettings _zboziShoppingSettings;
		private readonly CurrencySettings _currencySettings;
		private readonly ZboziProductObjectContext _objectContext;

		#endregion

		#region Ctor

		public ZboziShoppingService(IZboziService zboziService,
			IPriceCalculationService priceCalculationService,
			ITaxService taxService,
			IProductService productService,
			ICategoryService categoryService,
			IManufacturerService manufacturerService,
			IPictureService pictureService,
			ICurrencyService currencyService,
			ILanguageService languageService,
			ISettingService settingService,
			IWorkContext workContext,
			IMeasureService measureService,
			IDateRangeService dateRangeService,
			Nop.Services.Shipping.IShippingService shippingService,
			IShippingByWeightService shippingByWeightService,
			MeasureSettings measureSettings,
			ZboziShoppingSettings zbozihoppingSettings,
			CurrencySettings currencySettings,
			ZboziProductObjectContext objectContext)
		{
			this._shippingByWeightService = shippingByWeightService;
			this._shippingService = shippingService;
			this._dateRangeService = dateRangeService;
			this._zboziService = zboziService;
			this._priceCalculationService = priceCalculationService;
			this._taxService = taxService;
			this._productService = productService;
			this._categoryService = categoryService;
			this._manufacturerService = manufacturerService;
			this._pictureService = pictureService;
			this._currencyService = currencyService;
			this._languageService = languageService;
			this._settingService = settingService;
			this._workContext = workContext;
			this._measureService = measureService;
			this._measureSettings = measureSettings;
			this._zboziShoppingSettings = zbozihoppingSettings;
			this._currencySettings = currencySettings;
			this._objectContext = objectContext;
		}

		#endregion

		#region Utilities
		/// <summary>
		/// Removes invalid characters
		/// </summary>
		/// <param name="input">Input string</param>
		/// <param name="isHtmlEncoded">A value indicating whether input string is HTML encoded</param>
		/// <returns>Valid string</returns>
		private string StripInvalidChars(string input, bool isHtmlEncoded)
		{
			if (String.IsNullOrWhiteSpace(input))
				return input;

			//Microsoft uses a proprietary encoding (called CP-1252) for the bullet symbol and some other special characters, 
			//whereas most websites and data feeds use UTF-8. When you copy-paste from a Microsoft product into a website, 
			//some characters may appear as junk. Our system generates data feeds in the UTF-8 character encoding, 
			//which many shopping engines now require.

			//http://www.atensoftware.com/p90.php?q=182

			if (isHtmlEncoded)
				input = HttpUtility.HtmlDecode(input);

			input = input.Replace("¼", "");
			input = input.Replace("½", "");
			input = input.Replace("¾", "");
			//input = input.Replace("•", "");
			//input = input.Replace("”", "");
			//input = input.Replace("“", "");
			//input = input.Replace("’", "");
			//input = input.Replace("‘", "");
			//input = input.Replace("™", "");
			//input = input.Replace("®", "");
			//input = input.Replace("°", "");

			if (isHtmlEncoded)
				input = HttpUtility.HtmlEncode(input);

			return input;
		}
		private Currency GetUsedCurrency()
		{
			var currency = _currencyService.GetCurrencyById(_zboziShoppingSettings.CurrencyId);
			if (currency == null || !currency.Published)
				currency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
			return currency;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets a route for provider configuration
		/// </summary>
		/// <param name="actionName">Action name</param>
		/// <param name="controllerName">Controller name</param>
		/// <param name="routeValues">Route values</param>
		public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
		{
			actionName = "Configure";
			controllerName = "FeedZbozi";
			routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Feed.Zbozi.Controllers" }, { "area", null } };
		}

		/// <summary>
		/// Generate a feed
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <param name="store">Store</param>
		/// <returns>Generated feed</returns>
		public void GenerateFeed(Stream stream, Store store)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (store == null)
				throw new ArgumentNullException("store");

			const string zboziBaseNamespace = "http://www.zbozi.cz/ns/offer/1.0";

			var settings = new XmlWriterSettings
			{
				Encoding = Encoding.UTF8
			};

			//language
			var languageId = 0;
			var languages = _languageService.GetAllLanguages(storeId: store.Id);
			//if we have only one language, let's use it
			if (languages.Count == 1)
			{
				//let's use the first one
				var language = languages.FirstOrDefault();
				languageId = language != null ? language.Id : 0;
			}
			//otherwise, use the current one
			if (languageId == 0)
				languageId = _workContext.WorkingLanguage.Id;

			//we load all google products here using one SQL request (performance optimization)
			var allzboziProducts = _zboziService.GetAll();

			var shippingMethodList = _shippingService.GetAllShippingMethods(27);


			using (var writer = XmlWriter.Create(stream, settings))
			{
				//Generate feed according to the following specs: https://napoveda.seznam.cz/cz/zbozi/specifikace-xml-pro-obchody/specifikace-xml-feedu/
				writer.WriteStartDocument();
				writer.WriteStartElement("SHOP", zboziBaseNamespace);
				//writer.WriteAttributeString("xmlns", zboziBaseNamespace);
				//writer.WriteAttributeString("xmlns", "", null, zboziBaseNamespace);

				var products1 = _productService.SearchProducts(storeId: store.Id, visibleIndividuallyOnly: true);
				foreach (var product1 in products1)
				{
					var productsToProcess = new List<Product>();
					switch (product1.ProductType)
					{
						case ProductType.SimpleProduct:
							{
								//simple product doesn't have child products
								productsToProcess.Add(product1);
							}
							break;
						case ProductType.GroupedProduct:
							{
								//grouped products could have several child products
								var associatedProducts = _productService.GetAssociatedProducts(product1.Id, store.Id);
								productsToProcess.AddRange(associatedProducts);
							}
							break;
						default:
							continue;
					}
					foreach (var product in productsToProcess)
					{
						writer.WriteStartElement("SHOPITEM");

						#region Basic Product Information

						//id [id]- An identifier of the item
						writer.WriteElementString("ITEM_ID", product.Id.ToString());

						//title [title] - Title of the item
						writer.WriteStartElement("PRODUCT");
						var title = product.GetLocalized(x => x.Name, languageId);
						//title should be not longer than 70 characters
						if (title.Length > 70)
							title = title.Substring(0, 70);
						writer.WriteCData(title);
						writer.WriteEndElement(); // title

						//description [description] - Description of the item
						writer.WriteStartElement("DESCRIPTION");
						string description = product.GetLocalized(x => x.FullDescription, languageId);
						if (String.IsNullOrEmpty(description))
							description = product.GetLocalized(x => x.ShortDescription, languageId);
						if (String.IsNullOrEmpty(description))
							description = product.GetLocalized(x => x.Name, languageId); //description is required
																						 //resolving character encoding issues in your data feed
						description = StripInvalidChars(description, true);
						writer.WriteCData(description);
						writer.WriteEndElement(); // description



						//google product category [google_product_category] - Google's category of the item
						//the category of the product according to Google’s product taxonomy. http://www.google.com/support/merchants/bin/answer.py?answer=160081
						string googleProductCategory = "";
						//var googleProduct = _googleService.GetByProductId(product.Id);
						var zboziProduct = allzboziProducts.FirstOrDefault(x => x.ProductId == product.Id);
						if (zboziProduct != null)
							googleProductCategory = zboziProduct.Taxonomy;
						if (String.IsNullOrEmpty(googleProductCategory))
							googleProductCategory = _zboziShoppingSettings.DefaultGoogleCategory;
						if (String.IsNullOrEmpty(googleProductCategory))
							throw new NopException("Není nastavena základní kategorie");
						writer.WriteStartElement("CATEGORYTEXT");
						writer.WriteCData(googleProductCategory);
						writer.WriteFullEndElement(); // g:google_product_category

						string zboziCPC = "";
						if (zboziProduct != null)
							zboziCPC = zboziProduct.MAX_CPC;

						if (!string.IsNullOrEmpty(zboziCPC))
						{
							decimal result = 0;
							if (decimal.TryParse(zboziCPC, out result))
							{

								writer.WriteElementString("MAX_CPC", zboziCPC);
							}
							else
							{
								throw new Exception("Parametr MAX_CPC musí obsahovat číselnou hodnotu. ");
							}

						}

						string zboziCPC_serach = "";
						if (zboziProduct != null)
							zboziCPC_serach = zboziProduct.MAX_CPC_SEARCH;

						if (!string.IsNullOrEmpty(zboziCPC_serach))
						{
							decimal result = 0;
							if (decimal.TryParse(zboziCPC_serach, out result))
							{

								writer.WriteElementString("MAX_CPC_SEARCH", zboziCPC_serach);
							}
							else
							{
								throw new Exception("Parametr MAX_CPC_SEARCH musí obsahovat číselnou hodnotu. ");
							}

						}



						string EXTRA_MESSAGE = "";
						if (zboziProduct != null)
							EXTRA_MESSAGE = zboziProduct.EXTRA_MESSAGE;

						if (!string.IsNullOrEmpty(EXTRA_MESSAGE))
						{
							writer.WriteElementString("EXTRA_MESSAGE", EXTRA_MESSAGE);

						}




						string productName = "";
						if (zboziProduct != null)
							productName = zboziProduct.ProductName;

						if (!string.IsNullOrEmpty(productName))
						{
							writer.WriteStartElement("PRODUCTNAME");
							writer.WriteCData(productName);
							writer.WriteFullEndElement(); // 

						}
						else
						{
							writer.WriteStartElement("PRODUCTNAME");
							writer.WriteCData(title);
							writer.WriteFullEndElement(); // 

						}



						// params
						var parameters = "";
						if (zboziProduct != null)
							parameters = zboziProduct.Params;

						if (string.IsNullOrEmpty(parameters) && product.ProductSpecificationAttributes.Count() > 0)
						{
							// progenerujeme vsechny parametry
							foreach (var productSpecificAtribut in product.ProductSpecificationAttributes)
							{

								var option = productSpecificAtribut.SpecificationAttributeOption;
								var name = option.SpecificationAttribute.Name;
								var value = option.Name;

								writer.WriteStartElement("PARAM");

								writer.WriteStartElement("PARAM_NAME");
								writer.WriteString(name);
								writer.WriteEndElement();

								writer.WriteStartElement("VAL");
								writer.WriteString(value);
								writer.WriteEndElement();

								writer.WriteFullEndElement(); // g:


								//var values = mapping.ProductAttributeValues.Where(e=>e.)
							}
						}
						else
						{
							// prepiseme hodnotu

						}


						////product type [product_type] - Your category of the item
						//var defaultProductCategory = _categoryService
						//    .GetProductCategoriesByProductId(product.Id, store.Id)
						//    .FirstOrDefault();
						//if (defaultProductCategory != null)
						//{
						//    //TODO localize categories
						//    var category = defaultProductCategory.Category
						//        .GetFormattedBreadCrumb(_categoryService, separator: ">", languageId: languageId);
						//    if (!String.IsNullOrEmpty((category)))
						//    {
						//        writer.WriteStartElement("g", "product_type", googleBaseNamespace);
						//        writer.WriteCData(category);
						//        writer.WriteFullEndElement(); // g:product_type
						//    }
						//}

						//link [link] - URL directly linking to your item's page on your website
						var productUrl = string.Format("{0}{1}", store.Url, product.GetSeName(languageId));
						writer.WriteElementString("URL", productUrl);

						//image link [image_link] - URL of an image of the item
						//additional images [additional_image_link]
						//up to 10 pictures
						const int maximumPictures = 10;
						var pictures = _pictureService.GetPicturesByProductId(product.Id, maximumPictures);
						for (int i = 0; i < pictures.Count; i++)
						{
							var picture = pictures[i];
							var imageUrl = _pictureService.GetPictureUrl(picture,
								_zboziShoppingSettings.ProductPictureSize,
								storeLocation: store.Url);

							if (i == 0)
							{
								//default image
								writer.WriteElementString("IMGURL", imageUrl);
							}
							//else
							//{
							//	//additional image
							//	writer.WriteElementString("IMGURL_ALTERNATIVE", imageUrl);
							//}
						}
						if (!pictures.Any())
						{
							//no picture? submit a default one
							var imageUrl = _pictureService.GetDefaultPictureUrl(_zboziShoppingSettings.ProductPictureSize, storeLocation: store.Url);
							writer.WriteElementString("IMGURL", imageUrl);
						}


						#endregion

						#region Availability & Price


						//price [price] - Price of the item
						var currency = GetUsedCurrency();
						decimal finalPriceBase;
						if (_zboziShoppingSettings.PricesConsiderPromotions)
						{
							var minPossiblePrice = _priceCalculationService.GetFinalPrice(product, _workContext.CurrentCustomer);

							if (product.HasTierPrices)
							{
								//calculate price for the maximum quantity if we have tier prices, and choose minimal
								minPossiblePrice = Math.Min(minPossiblePrice,
									_priceCalculationService.GetFinalPrice(product, _workContext.CurrentCustomer, quantity: int.MaxValue));
							}

							decimal taxRate;
							finalPriceBase = _taxService.GetProductPrice(product, minPossiblePrice, out taxRate);
						}
						else
						{
							finalPriceBase = product.Price;
						}
						decimal price = _currencyService.ConvertFromPrimaryStoreCurrency(finalPriceBase, currency);
						//round price now so it matches the product details page
						price = RoundingHelper.RoundPrice(price);

						//writer.WriteElementString("g", "price", googleBaseNamespace,
						//                          price.ToString(new CultureInfo("en-US", false).NumberFormat) + " " +
						//                          currency.CurrencyCode);

					
							if(price == 0)
							{
								writer.WriteElementString("PRICE_VAT", "0");

							}
							else
							{
								writer.WriteElementString("PRICE_VAT", price.ToString("#,##"));

							}
						
							#endregion

						#region Unique Product Identifiers

						/* Unique product identifiers such as UPC, EAN, JAN or ISBN allow us to show your listing on the appropriate product page. If you don't provide the required unique product identifiers, your store may not appear on product pages, and all your items may be removed from Product Search.
                         * We require unique product identifiers for all products - except for custom made goods. For apparel, you must submit the 'brand' attribute. For media (such as books, movies, music and video games), you must submit the 'gtin' attribute. In all cases, we recommend you submit all three attributes.
                         * You need to submit at least two attributes of 'brand', 'gtin' and 'mpn', but we recommend that you submit all three if available. For media (such as books, movies, music and video games), you must submit the 'gtin' attribute, but we recommend that you include 'brand' and 'mpn' if available.
                        */

						//GTIN [gtin] - GTIN
						var gtin = product.Gtin;
						if (!String.IsNullOrEmpty(gtin))
						{
							writer.WriteStartElement("EAN");
							writer.WriteCData(gtin);
							writer.WriteFullEndElement(); // g:gtin
						}

						//brand [brand] - Brand of the item
						var defaultManufacturer =
							_manufacturerService.GetProductManufacturersByProductId((product.Id)).FirstOrDefault();
						if (defaultManufacturer != null)
						{
							writer.WriteStartElement("MANUFACTURER");
							writer.WriteCData(defaultManufacturer.Manufacturer.Name);
							writer.WriteFullEndElement(); // g:brand
						}


						//mpn [mpn] - Manufacturer Part Number (MPN) of the item
						var mpn = product.ManufacturerPartNumber;
						if (!String.IsNullOrEmpty(mpn))
						{
							writer.WriteStartElement("PRODUCTNO");
							writer.WriteCData(mpn);
							writer.WriteFullEndElement(); // g:mpn
						}

						//identifier exists [identifier_exists] - Submit custom goods
						//if (googleProduct != null && googleProduct.CustomGoods)
						//{
						//    writer.WriteElementString("g", "identifier_exists", googleBaseNamespace, "FALSE");
						//}

						#endregion

						#region Apparel Products




						#endregion

						#region Tax & Shipping


						string deliveryDate = "0";

						if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock
							&& product.BackorderMode == BackorderMode.NoBackorders
							&& product.GetTotalStockQuantity() <= 0)
						{
							// neni skladem a ma dobu pro doruceni
							if (product.DeliveryDateId > 0)
							{
								var deliveryDateRecord = _dateRangeService.GetDeliveryDateById(product.DeliveryDateId);
								deliveryDate = deliveryDateRecord.Name;
							}
							else
							{
								deliveryDate = _zboziShoppingSettings.DefaultDeliveryDate;
							}

						}

						writer.WriteStartElement("DELIVERY_DATE");
						writer.WriteString(deliveryDate);
						writer.WriteFullEndElement(); // 




						#endregion

						writer.WriteEndElement(); // item
					}
				}


				writer.WriteEndElement(); // shop
				writer.WriteEndDocument();
			}
		}


		/// <summary>
		/// Install plugin
		/// </summary>
		public override void Install()
		{
			//settings
			var settings = new ZboziShoppingSettings
			{
				PricesConsiderPromotions = false,
				ProductPictureSize = 125,
				PassShippingInfoWeight = false,
				PassShippingInfo = false,
				StaticFileName = string.Format("zbozi_feed_{0}.xml", CommonHelper.GenerateRandomDigitCode(10)),
				ExpirationNumberOfDays = 28
			};
			_settingService.SaveSetting(settings);

			//data
			_objectContext.Install();

			//locales

			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Products.EXTRAMESSAGE", "EXTRA MESSAGE ");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Products.MAXCPC", "MAX CPC");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Products.MAXCPC_SEARCH", "MAX CPC SEARCH");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Products.ProductName", "Product Name");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Products.ZboziProductName", "Product Name");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Store", "Obchod");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Store.Hint", "Zvolte obchod, pro který chcete generovat produktový feed.");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Currency", "Měna");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Currency.Hint", "Zvolte měnu, která bude použita v produktovém feedu.");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.DefaultGoogleCategory", "Defaultní kategorie zboži.cz");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.DefaultGoogleCategory.Hint", "Není nastavena základní kategorie.");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.General", "Základní nastavení");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.GeneralInstructions", "<p><ul><li>Před využitím modulu je nutné mít vytvořený účet na serveru zbozi.cz <a href=\"https://klient.seznam.cz/registration/zbozi/\" target=\"_blank\">https://klient.seznam.cz/registration/zbozi/</a></li><li>Pricip funkčnosti a aktualizace naleznete na stránce <a href=\"http://tomas-horvath.cz/nopcommerce-zbozi-cz-modul-instalce/\" target=\"_blank\">http://tomas-horvath.cz/nopcommerce-zbozi-cz-modul-instalce/</a></li><li><strong>Verze modulu :</strong> 1.0.0 <br /> Více informací o produktovém feedu  naleznete na <a href=\"https://napoveda.seznam.cz/cz/zbozi/specifikace-xml-pro-obchody/specifikace-xml-feedu/\" target=\"_blank\">https://napoveda.seznam.cz</a></li><li>Pro validaci produktového feedu můžete využít službu <a href=\"https://www.zbozi.cz/validace-feedu/\" target=\"_blank\">https://www.zbozi.cz/validace-feedu/</a></li></ul></p>");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Generate", "Vygenerovat produktový feed");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Override", "Přepsat nastavení produktu");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.OverrideInstructions", "<p>Aktuální strom kategorií naleznete vždy na této adrese: <a href=\"https://www.zbozi.cz/static/categories.csv\" target=\"_blank\">zde</a></p>");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.ProductPictureSize", "Velikost obrázku produktového náhledu");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.ProductPictureSize.Hint", "Velikost náhledového obrázku v jednotce pixel.");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Products.ProductName", "Produkt");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.Products.ZboziCategory", "Zboží.cz kategorie");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.SuccessResult", "Zboži.cz produktový feed byl úspěšně vygenerován.");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.StaticFilePath", "Cesta k produktovému feedu");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.StaticFilePath.Hint", "Cesta k produktovému feedu. Jedná se o URL adresu, kterou je nutné vložit do systému heuréka.cz");
			this.AddOrUpdatePluginLocaleResource("Plugins.Feed.Zbozi.DefaultDeliveryDate", "počet dnů pro dodání zboží pokud není skladem");

			


			base.Install();
		}

		/// <summary>
		/// Uninstall plugin
		/// </summary>
		public override void Uninstall()
		{
			//settings
			_settingService.DeleteSetting<ZboziShoppingSettings>();

			//data
			_objectContext.Uninstall();

			//locales
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Products.EXTRAMESSAGE");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Products.MAXCPC");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Products.MAXCPC_SEARCH");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Products.ProductName");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Store");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Store.Hint");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Currency");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Currency.Hint");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.DefaultGoogleCategory");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.DefaultGoogleCategory.Hint");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.General");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.GeneralInstructions");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Generate");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Override");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.OverrideInstructions");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.ProductPictureSize");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.ProductPictureSize.Hint");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Products.ProductName");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Products.ZboziCategory");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.SuccessResult");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.StaticFilePath");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.StaticFilePath.Hint");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.Products.ZboziProductName");
			this.DeletePluginLocaleResource("Plugins.Feed.Zbozi.DefaultDeliveryDate");


			base.Uninstall();
		}

		/// <summary>
		/// Generate a static feed file
		/// </summary>
		/// <param name="store">Store</param>
		public virtual void GenerateStaticFile(Store store)
		{
			if (store == null)
				throw new ArgumentNullException("store");
			string filePath = Path.Combine(HttpRuntime.AppDomainAppPath, "content\\files\\exportimport", store.Id + "-" + _zboziShoppingSettings.StaticFileName);
			using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
			{
				GenerateFeed(fs, store);
			}
		}

		#endregion
	}
}
