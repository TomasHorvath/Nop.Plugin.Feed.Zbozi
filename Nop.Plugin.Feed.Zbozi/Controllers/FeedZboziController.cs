using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Stores;
using Nop.Core.Plugins;
using Nop.Plugin.Feed.Zbozi.Domain;
using Nop.Plugin.Feed.Zbozi.Models;
using Nop.Plugin.Feed.Zbozi.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Security;

namespace Nop.Plugin.Feed.Zbozi.Controllers
{
    [AdminAuthorize]
    public class FeedZboziController : BasePluginController
    {
        private readonly IZboziService _zboziService;
        private readonly IProductService _productService;
        private readonly ICurrencyService _currencyService;
        private readonly ILocalizationService _localizationService;
        private readonly IPluginFinder _pluginFinder;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly IStoreService _storeService;
        private readonly ZboziShoppingSettings _zboziSettings;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;

        public FeedZboziController(IZboziService zboziService,
            IProductService productService,
            ICurrencyService currencyService,
            ILocalizationService localizationService,
            IPluginFinder pluginFinder,
            ILogger logger,
            IWebHelper webHelper,
            IStoreService storeService,
            ZboziShoppingSettings ZboziSettings,
            ISettingService settingService,
            IPermissionService permissionService)
        {
            this._zboziService = zboziService;
            this._productService = productService;
            this._currencyService = currencyService;
            this._localizationService = localizationService;
            this._pluginFinder = pluginFinder;
            this._logger = logger;
            this._webHelper = webHelper;
            this._storeService = storeService;
            this._zboziSettings = ZboziSettings;
            this._settingService = settingService;
            this._permissionService = permissionService;
        }

        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new ZboziFeedModel();
            model.ProductPictureSize = _zboziSettings.ProductPictureSize;
            model.PassShippingInfoWeight = _zboziSettings.PassShippingInfoWeight;
            model.PassShippingInfo = _zboziSettings.PassShippingInfo;
            model.PricesConsiderPromotions = _zboziSettings.PricesConsiderPromotions;
		

			//stores
			model.StoreId = _zboziSettings.StoreId;
            model.AvailableStores.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            foreach (var s in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
            //currencies
            model.CurrencyId = _zboziSettings.CurrencyId;
            foreach (var c in _currencyService.GetAllCurrencies())
                model.AvailableCurrencies.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            //Google categories
            model.DefaultGoogleCategory = _zboziSettings.DefaultGoogleCategory;
            model.AvailableGoogleCategories.Add(new SelectListItem {Text = "Zvolte základní kategorie", Value = ""});
            foreach (var gc in _zboziService.GetTaxonomyList())
                model.AvailableGoogleCategories.Add(new SelectListItem { Text = gc, Value = gc });

            //file paths
            foreach (var store in _storeService.GetAllStores())
            {
                var localFilePath = System.IO.Path.Combine(HttpRuntime.AppDomainAppPath, "content\\files\\exportimport", store.Id + "-" + _zboziSettings.StaticFileName);
                if (System.IO.File.Exists(localFilePath))
                    model.GeneratedFiles.Add(new Zbozi.Models.ZboziFeedModel.GeneratedFileModel
                    {
                        StoreName = store.Name,
                        FileUrl = string.Format("{0}content/files/exportimport/{1}-{2}", _webHelper.GetStoreLocation(false), store.Id, _zboziSettings.StaticFileName)
                    });
            }

            return View("~/Plugins/Feed.Zbozi/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [ChildActionOnly]
        [FormValueRequired("save")]
        public ActionResult Configure(Zbozi.Models.ZboziFeedModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            //save settings
            _zboziSettings.ProductPictureSize = model.ProductPictureSize;
            _zboziSettings.PassShippingInfoWeight = model.PassShippingInfoWeight;
            _zboziSettings.PassShippingInfo = model.PassShippingInfo;
            _zboziSettings.PricesConsiderPromotions = model.PricesConsiderPromotions;
            _zboziSettings.CurrencyId = model.CurrencyId;
            _zboziSettings.StoreId = model.StoreId;
			_zboziSettings.DefaultDeliveryDate = model.DefaultDeliveryDate;
            _zboziSettings.DefaultGoogleCategory = model.DefaultGoogleCategory;
			


            _settingService.SaveSetting(_zboziSettings);

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            //redisplay the form
            return Configure();
        }

        [HttpPost, ActionName("Configure")]
        [ChildActionOnly]
        [FormValueRequired("generate")]
        public ActionResult GenerateFeed(Zbozi.Models.ZboziFeedModel model)
        {
            try
            {
                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("ProductFeed.Zbozi");
                if (pluginDescriptor == null)
                    throw new Exception("Cannot load the plugin");

                //plugin
                var plugin = pluginDescriptor.Instance() as ZboziShoppingService;
                if (plugin == null)
                    throw new Exception("Cannot load the plugin");

                var stores = new List<Store>();
                var storeById = _storeService.GetStoreById(_zboziSettings.StoreId);
                if (storeById != null)
                    stores.Add(storeById);
                else
                    stores.AddRange(_storeService.GetAllStores());

                foreach (var store in stores)
                    plugin.GenerateStaticFile(store);

                SuccessNotification(_localizationService.GetResource("Plugins.Feed.Zbozi.SuccessResult"));
            }
            catch (Exception exc)
            {
                ErrorNotification(exc.Message);
                _logger.Error(exc.Message, exc);
            }

            return Configure();
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult GoogleProductList(DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return ErrorForKendoGridJson("Access denied");

            var products = _productService.SearchProducts(pageIndex: command.Page - 1,
                pageSize: command.PageSize, showHidden: true);
            var productsModel = products
                .Select(x =>
                            {
                                var gModel = new Zbozi.Models.ZboziFeedModel.GoogleProductModel
                                {
                                    ProductId = x.Id,
                                    ProductName = x.Name

                                };
                                var googleProduct = _zboziService.GetByProductId(x.Id);
                                if (googleProduct != null)
                                {
                                    gModel.ZboziCategory = googleProduct.Taxonomy;
                                    gModel.Gender = googleProduct.Gender;
                                    gModel.AgeGroup = googleProduct.AgeGroup;
                                    gModel.Color = googleProduct.Color;
                                    gModel.GoogleSize = googleProduct.Size;
                                    gModel.CustomGoods = googleProduct.CustomGoods;
									gModel.EXTRAMESSAGE = googleProduct.EXTRA_MESSAGE;
									gModel.MAXCPC = googleProduct.MAX_CPC;
									gModel.ZboziProductName = googleProduct.ProductName;
									gModel.MAXCPC_SEARCH = googleProduct.MAX_CPC_SEARCH;
                                }

                                return gModel;
                            })
                .ToList();

            var gridModel = new DataSourceResult
            {
                Data = productsModel,
                Total = products.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult GoogleProductUpdate(Zbozi.Models.ZboziFeedModel.GoogleProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return Content("Access denied");

            var zboziProduct = _zboziService.GetByProductId(model.ProductId);
            if (zboziProduct != null)
            {

                zboziProduct.Taxonomy = model.ZboziCategory;
                zboziProduct.Gender = model.Gender;
                zboziProduct.AgeGroup = model.AgeGroup;
                zboziProduct.Color = model.Color;
                zboziProduct.Size = model.GoogleSize;
                zboziProduct.CustomGoods = model.CustomGoods;
				zboziProduct.MAX_CPC = model.MAXCPC;
				zboziProduct.MAX_CPC_SEARCH = model.MAXCPC_SEARCH;
				zboziProduct.ProductName = model.ZboziProductName;
				zboziProduct.EXTRA_MESSAGE = model.EXTRAMESSAGE;
			

                _zboziService.UpdateGoogleProductRecord(zboziProduct);
            }
            else
            {
                //insert
                zboziProduct = new ZboziProductRecord
                {
                    ProductId = model.ProductId,
                    Taxonomy = model.ZboziCategory,
					MAX_CPC = model.MAXCPC,
					EXTRA_MESSAGE = model.EXTRAMESSAGE,
					MAX_CPC_SEARCH = model.MAXCPC_SEARCH,
					ProductName = model.ZboziProductName,
                    Gender = model.Gender,
                    AgeGroup = model.AgeGroup,
                    Color = model.Color,
                    Size = model.GoogleSize,
                    CustomGoods = model.CustomGoods
                };
                _zboziService.InsertGoogleProductRecord(zboziProduct);
            }

            return new NullJsonResult();
        }
    }
}
