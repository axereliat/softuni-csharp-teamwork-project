using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Blog.Models
{
    public class EditUserViewModel
    {
        public ApplicationUser User { get; set; }
        public string Password { get; set; }

        [DisplayName("Confirm Password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        public IList<Role> Roles { get; set; }
    }
}