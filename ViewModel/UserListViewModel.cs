using System.Collections.Generic;
using CramsRBIApp.Models;

namespace CramsRBIApp.ViewModel
{
    public class UserListViewModel
    {
        public List<UserViewModel> Users { get; set; } = new List<UserViewModel>();
        public PaginationInfo PaginationInfo { get; set; } = new PaginationInfo();
        public string SearchString { get; set; }
        public string SortField { get; set; }
        public string SortOrder { get; set; }
        public string CurrentFilter { get; set; }
    }

    public class UserViewModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class PaginationInfo
    {
        public int TotalItems { get; set; }
        public int ItemsPerPage { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages => (int)Math.Ceiling((decimal)TotalItems / ItemsPerPage);
    }
}