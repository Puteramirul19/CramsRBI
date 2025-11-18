using CramsRBIApp.ViewModel;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CramsRBIApp.Controllers
{
    internal class PermissionViewModel
    {
        public List<SelectListItem> AvailableRoles { get; set; }
        public List<MenuPermissionItem> MenuPermissions { get; set; }
    }
}