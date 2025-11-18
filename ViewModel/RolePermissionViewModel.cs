// Create this file: ViewModel/PermissionViewModels.cs

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using CramsRBIApp.Models;

namespace CramsRBIApp.ViewModel
{
    public class RolePermissionViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
        public List<MenuPermissionItem> MenuPermissions { get; set; } = new List<MenuPermissionItem>();
    }

    public class MenuPermissionItem
    {
        public string MenuName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool CanEdit { get; set; } = false;
        public bool CanView { get; set; } = false;
    }

    public class PermissionMatrixViewModel
    {
        public List<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();
        public List<MenuPermissionItem> Menus { get; set; } = new List<MenuPermissionItem>();
        public Dictionary<string, Dictionary<string, bool>> PermissionMatrix { get; set; } = new Dictionary<string, Dictionary<string, bool>>();
    }
}