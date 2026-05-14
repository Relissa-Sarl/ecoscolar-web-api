using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EcoscolarWebApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AdvertsController : Controller
    {
        // GET: AdvertsController
        public ActionResult Index()
        {
            return View();
        }

        // GET: AdvertsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AdvertsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AdvertsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AdvertsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: AdvertsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: AdvertsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AdvertsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
