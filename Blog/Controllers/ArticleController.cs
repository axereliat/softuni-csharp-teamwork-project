﻿using Blog.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TimeAgo;

namespace Blog.Controllers
{
    public class ArticleController : Controller
    {
        // GET: Article
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        // GET: /Article/List
        public ActionResult List()
        {
            using (var database = new BlogDbContext())
            {
                var articles = database.Articles
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .Include(a => a.Category)
                    .ToList();

                return View(articles);
            }
        }

        public ActionResult Details(int? id) {
            if (id == null) {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext()) {
                var article = database.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .First();

                if (article == null) {
                    return HttpNotFound();
                }

                return View(article);
            }
        }

        // GET: /Article/Edit
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id)
                    .First();

                if (article == null)
                {
                    return HttpNotFound();
                }

                if (!IsUserAuthorizedToEdit(article))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                var model = new ArticleViewModel();
                model.Id = article.Id;
                model.Content = article.Content;
                model.Title = article.Title;
                model.CategoryId = article.CategoryId;
                model.Categories = database.Categories
                    .OrderBy(c => c.Name)
                    .ToList();

                model.Tags = string.Join(", ", article.Tags.Select(t => t.Name));

                return View(model);
            }
        }

        // POST: /Article/Edit
        [HttpPost]
        public ActionResult Edit(ArticleViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var database = new BlogDbContext())
                {
                    var article = database.Articles
                        .FirstOrDefault(a => a.Id == model.Id);


                    article.Id = model.Id;
                    article.Content = model.Content;
                    article.Title = model.Title;
                    article.CategoryId = model.CategoryId;
                    this.SetArticleTags(article, model, database);

                    database.Entry(article).State = EntityState.Modified;
                    database.SaveChanges();

                    return RedirectToAction("Index");
                }
            }

            return View(model);
        }

        private void SetArticleTags(Article article, ArticleViewModel model, BlogDbContext db)
        {
            var tagsStrings = model.Tags
                .Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.ToLower())
                .Distinct();

            article.Tags.Clear();

            foreach (var tagString in tagsStrings)
            {
                Tag tag = db.Tags.FirstOrDefault(t => t.Name.Equals(tagString));

                if (tag == null) {
                    tag = new Tag() { Name = tagString };
                    db.Tags.Add(tag);
                }

                article.Tags.Add(tag);
            }
        }

        // Get: Article/Create
        [Authorize]
        public ActionResult Create()
        {
            using (var database = new BlogDbContext())
            {
                var model = new ArticleViewModel();
                model.Categories = database.Categories
                    .OrderBy(c => c.Name)
                    .ToList();

                return View(model);
            }
        }

        // Post: Article/Create
        [HttpPost]
        [Authorize]
        public ActionResult Create(ArticleViewModel model) {
            if (ModelState.IsValid) {
                using (var database = new BlogDbContext())
                {
                    var authorId = database.Users
                        .Where(u => u.UserName == this.User.Identity.Name)
                        .First()
                        .Id;

                    var article = new Article(authorId, model.Title, model.Content, model.CategoryId);
                    
                    article.DateAdded = DateTime.Now;

                    this.SetArticleTags(article, model, database);

                    database.Articles.Add(article);
                    database.SaveChanges();

                    return RedirectToAction("Index");
                }
            }

            return View(model);
        }

        // Get: Article/Delete
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .Include(a => a.Category)
                    .First();

                if (article == null)
                {
                    return HttpNotFound();
                }

                if (!IsUserAuthorizedToEdit(article)) {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                ViewBag.TagsString = string.Join(", ", article.Tags.Select(t => t.Name));

                return View(article);
            }
        }

        // Post: Article/Delete
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .First();

                if (article == null)
                {
                    return HttpNotFound();
                }

                database.Articles.Remove(article);
                database.SaveChanges();

                return RedirectToAction("Index");
            }
        }

        private bool IsUserAuthorizedToEdit(Article article)
        {
            bool isAdmin = this.User.IsInRole("Admin");
            bool isAuthor = article.IsAuthor(this.User.Identity.Name);

            return isAdmin || isAuthor;
        }
    }
}