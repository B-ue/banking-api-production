using Microsoft.AspNetCore.Mvc;

namespace BankingTransactionApi.Controllers
{
    public class TransactionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}