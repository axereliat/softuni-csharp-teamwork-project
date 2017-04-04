using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Blog.Models
{
    public class ArticleViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Title { get; set; }

        [Required]
        [UIHint("tinymce_jquery_full"), AllowHtml]
        public string Content { get; set; }

        public string SearchTxt { get; set; }

        public string AuthorId { get; set; }

        public PagedList.IPagedList<Article> PagedArticles { get; set; }

        public int CategoryId { get; set; }
        public ICollection<Article> Articles { get; set; }
        public List<Category> Categories { get; set; }
        public Comment Comment { get; set; }

        public string Tags { get; set; }
    }
}