using Microsoft.Extensions.DependencyInjection;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.XA.Foundation.Abstractions;
using Sitecore.XA.Foundation.Multisite;
using System.Web;
using System.Web.Routing;
using Sitecore.Links;
using Sitecore.Web;

namespace Sitecore.Support.XA.Feature.ErrorHandling.Pipelines.HttpRequestBegin
{
  public class ItemNotFoundResolver : HttpRequestProcessor
  {
    protected IContext Context
    {
      get;
    } = ServiceLocator.ServiceProvider.GetService<IContext>();


    public override void Process(HttpRequestArgs args)
    {
      if (Context.Item == null && Context.Site != null && Context.Database != null && string.IsNullOrEmpty(Context.Page.FilePath) && RouteTable.Routes.GetRouteData(new HttpContextWrapper(args.Context)) == null && !args.PermissionDenied)
      {
        RedirectItemNotFound(args); // Fix #42073
      }
    }

    private void RedirectItemNotFound(HttpRequestArgs args)
    {
      Item settingsItem = ServiceLocator.ServiceProvider.GetService<IMultisiteContext>().GetSettingsItem(Context.Database.GetItem(Context.Site.StartPath));
      if (settingsItem != null)
      {
        var itemNotFoundItem = Context.Database.GetItem(settingsItem[Sitecore.XA.Feature.ErrorHandling.Templates._ErrorHandling.Fields.Error404Page] ?? "");
        string url = LinkManager.GetItemUrl(itemNotFoundItem);
        WebUtil.Redirect(url, true);
      }
    }
  }
}