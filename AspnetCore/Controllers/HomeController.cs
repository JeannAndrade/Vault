using Microsoft.AspNetCore.Mvc;

namespace AspnetCore.Controllers
{
  public class HomeController : Controller
  {
    public IActionResult Index() => View();
  }
}