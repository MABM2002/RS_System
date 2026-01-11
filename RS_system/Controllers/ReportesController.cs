using Rs_system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rs_system.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private readonly IReporteService _reporteService;

        public ReportesController(IReporteService reporteService)
        {
            _reporteService = reporteService;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
