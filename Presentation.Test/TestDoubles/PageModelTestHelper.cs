using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;

namespace Presentation.Test.TestDoubles;

internal static class PageModelTestHelper
{
    public static Microsoft.AspNetCore.Mvc.ActionContext CreateActionContext() =>
        new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

    public static PageContext CreatePageContext(Microsoft.AspNetCore.Mvc.ActionContext? actionContext = null) =>
        new(actionContext ?? CreateActionContext());
}
