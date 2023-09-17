using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using ToDoList.Models;

namespace ToDoList.Controllers
{
    public class HomeController : Controller
    {
        private ToDoContext _ctx;
        public HomeController(ToDoContext ctx) => _ctx = ctx;

        public ViewResult Index(string id)
        {
            //load current filter and data  needed for filter drop downs in ViewBag
            var filters = new Filters(id);
            ViewBag.Filters = filters;
            ViewBag.Categories = _ctx.Categories.ToList();
            ViewBag.Status =_ctx.Statuses.ToList();
            ViewBag.DueFilters = Filters.DueFilterValues;

            //got open tasks from database based on current filter
            IQueryable<ToDo> query = _ctx.ToDos
                .Include(t => t.Category).Include(t => t.Status);
            
            if(filters.HasCategory)
                query = query.Where(t=>t.CategoryId == filters.CategoryId);
            if(filters.HasStatus)
                query =query.Where(t=>t.StatusId == filters.StatusId);
            if (filters.HasDue)
            {
                var today = DateTime.Today;
                if (filters.IsPast)
                    query = query.Where(t => t.DueDate < today);
                else if (filters.IsFuture)
                    query = query.Where(t => t.DueDate > today);
                else if (filters.IsToday)
                    query = query.Where(t => t.DueDate == today);
            }
            var tasks = query.OrderBy(t => t.DueDate).ToList();
            return View(tasks);
        }

        [HttpGet]
        public ViewResult Add()
        {
            ViewBag.Categories = _ctx.Categories.ToList();
            ViewBag.Statuses = _ctx.Statuses.ToList();
            var task = new ToDo { StatusId = "Open" };//Set Default value for drop down
            return View(task);
        }
        [HttpPost]
        public IActionResult Add(ToDo task)
        {
            if (ModelState.IsValid)
            {
                _ctx.ToDos.Add(task);
                _ctx.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Categories = _ctx.Categories.ToList();
                ViewBag.Statuses = _ctx.Statuses.ToList();
                return View(task);
            }
        }
        [HttpPost]
        public IActionResult Filter(string[] filter)
        {
            string id = string.Join('-', filter);
            return RedirectToAction("Index", new { ID = id });
        }
        [HttpPost]
        public IActionResult MarkComplete([FromRoute] string id, ToDo Selected)
        {
            Selected = _ctx.ToDos.Find(Selected.Id)!; //use null-forgiving operator to supress null warning 
            if (Selected != null)
            {
                Selected.StatusId = "closed";
                _ctx.SaveChanges();
            }
            return RedirectToAction("Index", new {ID = id});
        }
        [HttpPost]
        public IActionResult DeleteComplete(string id) {
            var toDelete = _ctx.ToDos
                .Where(t=>t.StatusId == "closed").ToList(); 
            foreach(var task in toDelete)
            {
                _ctx.ToDos.Remove(task);
            }
            _ctx.SaveChanges();
            return RedirectToAction("Index", new { ID = id });
        }

    }
}
