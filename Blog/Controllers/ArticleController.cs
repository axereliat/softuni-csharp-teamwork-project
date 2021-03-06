﻿using Blog.Models;
using Microsoft.AspNet.Identity;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
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
        public ActionResult List(int page = 1, int pageSize = 4)
        {
            using (var database = new BlogDbContext())
            {
                var articles = database.Articles
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .Include(a => a.Category)
                    .ToList();

                var model = new PagedList<Article>(articles, page, pageSize);
                //return View(model);
                return Redirect("/Home/ListCategories");
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
                    .Include(a => a.Comments.Select(c => c.Author))
                    .Include(a => a.UsersLikes)
                    .FirstOrDefault();

                var user = database.Users
                    .Where(a => a.Email == this.User.Identity.Name)
                    .FirstOrDefault();

                if (article.UsersLikes.Contains(user))
                {
                    ViewBag.hasUserLiked = true;
                }
                else {
                    ViewBag.hasUserLiked = false;
                }

                if (article == null) {
                    return HttpNotFound();
                }

                article.viewCounter++;

                database.Entry(article).State = EntityState.Modified;
                database.SaveChanges();

                return View(article);
            }
        }

        [Authorize]
        public ActionResult Like(int? id, string userName)
        {
            if (id == null || userName == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .Include(a => a.UsersLikes)
                    .Include(a => a.Comments.Select(c => c.Author))
                    .FirstOrDefault();


                var user = database.Users
                    .Where(a => a.Email == userName)
                    .FirstOrDefault();

                if (article == null || user == null)
                {
                    return HttpNotFound();
                }
                
                
                if (!article.UsersLikes.Contains(user))
                {
                    article.LikesCount++;
                    article.UsersLikes.Add(user);
                }
                else {
                    article.LikesCount--;
                    article.UsersLikes.Remove(user);
                }

                database.Entry(article).State = EntityState.Modified;
                database.SaveChanges();

                return Redirect($"/Article/Details/{id}");
            }
        }

        [HttpPost]
        [Authorize]
        [ActionName("Details")]
        public ActionResult DetailsConfirmed(int? id, Article article1)
        {
            if (!ModelState.IsValid) {
                return RedirectToAction("asdas");
            }
            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .Include(a => a.Comments)
                    .FirstOrDefault();
                var userId = database.Users
                        .Where(u => u.UserName == this.User.Identity.Name)
                        .First()
                        .Id;
                var user = database.Users
                        .Where(u => u.UserName == this.User.Identity.Name)
                        .First();

                var comment = new Comment();
                comment.DateAdded = DateTime.Now;
                comment.AuthorId = userId;
                comment.Author = user;
                comment.ArticleId = article.Id;
                comment.Content = article1.CurrentComment;

                if (article.Author.UserName == this.User.Identity.Name) {
                    comment.Seen = true;
                }

                article.Comments.Add(comment);
                
                database.Entry(article).State = EntityState.Modified;
                database.Comments.Add(comment);
                database.SaveChanges();

                ModelState.Clear();

                return Redirect($"/Article/Details/{id}");
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
        public ActionResult Edit(ArticleViewModel model, HttpPostedFileBase file)
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

                    // file upload

                    ViewBag.isSuccesfulThumbUpload = false;
                    if (file != null)
                    {
                        if (file.ContentLength > 0)
                        {
                            if (Path.GetExtension(file.FileName).ToLower() == ".jpg")
                            {
                                var fileName = "article_" + article.Id;
                                fileName += Path.GetExtension(file.FileName);
                                var path = Path.Combine(Server.MapPath("~/Content/Articles"), fileName);
                                file.SaveAs(path);
                                ViewBag.isSuccesfulThumbUpload = true;
                            }
                        }
                    }
                    if (ViewBag.isSuccesfulThumbUpload == false && file != null)
                    {
                        return Redirect($"/Article/Edit/{model.Id}");
                    }

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
        public ActionResult Create(ArticleViewModel model, HttpPostedFileBase file) {
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

                    // file upload

                    ViewBag.isSuccesfulThumbUpload = false;
                    if (file != null)
                    {
                        if (file.ContentLength > 0)
                        {
                            if (Path.GetExtension(file.FileName).ToLower() == ".jpg")
                            {
                                var fileName = "article_" + article.Id;
                                fileName += Path.GetExtension(file.FileName);
                                var path = Path.Combine(Server.MapPath("~/Content/Articles"), fileName);
                                file.SaveAs(path);
                                ViewBag.isSuccesfulThumbUpload = true;
                            }
                        }
                    }
                    if (ViewBag.isSuccesfulThumbUpload == false)
                    {
                        return Redirect("/Create");
                    }

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

        // Get: DeleteComment
        public ActionResult DeleteComment(int? id, int? articleId)
        {
            if (id == null || articleId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                var comment = database.Comments
                    .FirstOrDefault(c => c.Id == id);
                var article = database.Articles
                    .FirstOrDefault(a => a.Id == articleId);

                if (comment == null || article == null)
                {
                    return HttpNotFound();
                }

                database.Comments.Remove(comment);
                database.SaveChanges();

                return Redirect($"/Article/Details/{articleId}");
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