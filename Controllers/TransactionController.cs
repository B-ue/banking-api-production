using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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