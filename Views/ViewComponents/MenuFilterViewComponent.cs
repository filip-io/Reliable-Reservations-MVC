using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Reliable_Reservations_MVC.Models.Menu;

namespace Reliable_Reservations_MVC.Views.ViewComponents
{
    public class MenuFilterViewComponent : ViewComponent
    {
        private readonly IMenuService _menuService;

        public MenuFilterViewComponent(IMenuService menuService)
        {
            _menuService = menuService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string category = null, decimal? minPrice = null, decimal? maxPrice = null)
        {
            var filteredMenuItems = await _menuService.GetFilteredMenuItems(category, minPrice, maxPrice);
            return View(filteredMenuItems);
        }
    }
}
