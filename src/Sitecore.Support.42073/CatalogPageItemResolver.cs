using Sitecore.Commerce.XA.Foundation.Catalog.Managers;
using Sitecore.Commerce.XA.Foundation.Common.Context;
using Sitecore.Commerce.XA.Foundation.Common.Providers;
using Sitecore.Commerce.XA.Foundation.Common.Utils;
using Sitecore.Commerce.XA.Foundation.Connect.Managers;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using System;
using System.Web;
using Sitecore.Pipelines.HttpRequest;

namespace Sitecore.Support.Commerce.XA.Foundation.Catalog.Pipelines
{
  public class CatalogPageItemResolver
  {
    public IContext Context { get; }

    public IStorefrontContext StorefrontContext { get; set; }

    public ISearchManager SearchManager { get; set; }

    public IItemTypeProvider ItemTypeProvider { get; set; }

    public ICatalogUrlManager CatalogUrlManager { get; set; }

    public ISiteContext SiteContext { get; set; }

    public CatalogPageItemResolver()
    {
      SearchManager = ServiceLocatorHelper.GetService<ISearchManager>();
      Assert.IsNotNull(SearchManager, "this.SearchManager service could not be located.");
      StorefrontContext = ServiceLocatorHelper.GetService<IStorefrontContext>();
      Assert.IsNotNull(StorefrontContext, "this.StorefrontContext service could not be located.");
      ItemTypeProvider = ServiceLocatorHelper.GetService<IItemTypeProvider>();
      Assert.IsNotNull(ItemTypeProvider, "this.ItemTypeProvider service could not be located.");
      CatalogUrlManager = ServiceLocatorHelper.GetService<ICatalogUrlManager>();
      Assert.IsNotNull(CatalogUrlManager, "this.CatalogUrlManager service could not be located.");
      SiteContext = ServiceLocatorHelper.GetService<ISiteContext>();
      Assert.IsNotNull(SiteContext, "this.SiteContext service could not be located.");
      Context = ServiceLocatorHelper.GetService<IContext>();
      Assert.IsNotNull(SiteContext, "this.SitecoreContext service could not be located.");
    }

    public virtual void Process(HttpRequestArgs args)
    {
      if (Context.Item != null && SiteContext.CurrentCatalogItem == null)
      {
        Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes contextItemType = GetContextItemType();
        if (contextItemType == Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Category ||
            contextItemType == Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Product)
        {
          string catalogItemIdFromUrl = GetCatalogItemIdFromUrl();
          if (!string.IsNullOrEmpty(catalogItemIdFromUrl))
          {
            bool isProduct = contextItemType ==
                             Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Product;
            string catalog = StorefrontContext.CurrentStorefront.Catalog;
            Item item = ResolveCatalogItem(catalogItemIdFromUrl, catalog, isProduct);
            if (item == null)
            {
              Context.Item = null; // Fix #42073
            }
            else
              SiteContext.CurrentCatalogItem = item;
          }
          else if (!Context.IsExperienceEditor)
            Context.Item = null; // Fix #42073
        }
      }
    }

    protected virtual Item ResolveCatalogItem(string itemId, string catalogName, bool isProduct)
    {
      Item result = null;
      if (!string.IsNullOrEmpty(itemId))
      {
        result = ((!isProduct)
            ? SearchManager.GetCategory(itemId, catalogName)
            : SearchManager.GetProduct(itemId, catalogName));
      }
      return result;
    }

    private Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes GetContextItemType()
    {
      Template template = TemplateManager.GetTemplate(Sitecore.Context.Item);
      Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes result =
          Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Unknown;
      if (template.InheritsFrom(Sitecore.Commerce.XA.Foundation.Common.Constants.DataTemplates.CategoryPage.ID))
      {
        result = Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Category;
      }
      else if (template.InheritsFrom(Sitecore.Commerce.XA.Foundation.Common.Constants.DataTemplates.ProductPage.ID))
      {
        result = Sitecore.Commerce.XA.Foundation.Common.Constants.ItemTypes.Product;
      }
      return result;
    }

    private string GetCatalogItemIdFromUrl()
    {
      string result = string.Empty;
      string rawUrl = HttpContext.Current.Request.RawUrl;
      int num = rawUrl.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
      if (num > 0)
      {
        rawUrl = rawUrl.Substring(num + 1);
        num = rawUrl.IndexOf("?", StringComparison.OrdinalIgnoreCase);
        if (num > 0)
        {
          rawUrl = rawUrl.Substring(0, num);
        }
        result = CatalogUrlManager.ExtractItemId(rawUrl);
      }
      return result;
    }
  }
}