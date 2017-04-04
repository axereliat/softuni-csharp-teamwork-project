using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;

namespace Blog.Models
{
    public class Article
    {
        private ICollection<Tag> tags;

        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public string Title { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }

        public virtual Category Category { get; set; }

        public virtual string CurrentComment { get; set; }

        public string Content { get; set; }

        public virtual ICollection<Tag> Tags {
            get { return this.tags; }
            set { this.tags = value; }
        }

        public virtual ICollection<Comment> Comments { get; set; }

        public DateTime DateAdded { get; set; }

        [ForeignKey("Author")]
        public string AuthorId { get; set; }
        
        public virtual ApplicationUser Author { get; set; }

        public int viewCounter { get; set; }

        public int LikesCount { get; set; }

        public virtual ICollection<ApplicationUser> UsersLikes { get; set; }

        public bool IsAuthor(string name) {
            return this.Author.UserName.Equals(name);
        }

        public Article() {
            this.tags = new HashSet<Tag>();
            this.Comments = new HashSet<Comment>();
        }

        public Article(string authorId, string title, string content, int categoryId) {
            this.AuthorId = authorId;
            this.Title = title;
            this.Content = content;
            this.CategoryId = categoryId;
            this.tags = new HashSet<Tag>();
            this.Comments = new HashSet<Comment>();
        }
    }
}