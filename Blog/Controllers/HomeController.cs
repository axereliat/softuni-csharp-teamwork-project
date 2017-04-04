using Blog.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Blog.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("ListCategories");
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase file)
        {
            if (file != null)
            {
                if (file.ContentLength > 0)
                {
                    if (Path.GetExtension(file.FileName).ToLower() == ".jpg"
                        || Path.GetExtension(file.FileName).ToLower() == ".png"
                        || Path.GetExtension(file.FileName).ToLower() == ".jpeg")
                    {
                        var path = Path.Combine(Server.MapPath("~/Content/Avatars"), file.FileName);
                        file.SaveAs(path);
                    }
                }
            }
            return View();
        }

        public ActionResult ListCategories()
        {
            using (var database = new BlogDbContext())
            {
                var categories = database.Categories
                    .Include(c => c.Articles)
                    .OrderBy(c => c.Name)
                    .ToList();

                return View(categories);
            }
        }

        [Authorize]
        public ActionResult MyArticles(int page = 1, int pageSize = 4)
        {
            using (var database = new BlogDbContext())
            {
                var articles = database.Articles
                    .Where(a => a.Author.Email == User.Identity.Name)
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .Include(a => a.Category)
                    .ToList();
                
                var model = new PagedList<Article>(articles, page, pageSize);
                return View(model);
            }
        }

        //[HttpGet]
        public ActionResult ListArticles(int? categoryId, string SearchTxt, int page = 1, int pageSize = 4)
        {
            ViewBag.SearchErrorMsg = "";
            if (SearchTxt == null) {
                SearchTxt = "";
            }
            if (categoryId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.currentCategoryId = categoryId;

            using (var database = new BlogDbContext())
            {
                var articles = database.Articles
                    .Where(a => a.CategoryId == categoryId && a.Title.Contains(SearchTxt))
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .ToList();

                if (!articles.Any())
                {
                    ViewBag.SearchErrorMsg = "No articles...";
                }

                var model1 = new ArticleViewModel();

                var model = new PagedList<Article>(articles, page, pageSize);
                model1.PagedArticles = model;
                return View(model1);
            }
        }
    }
}