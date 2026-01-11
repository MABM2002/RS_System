using System.Collections.Generic;

namespace Rs_system.Models.ViewModels
{
    public class MenuViewModel
    {
        public List<MenuItem> Items { get; set; } = new List<MenuItem>();
    }

    public class MenuItem
    {
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Url { get; set; }
        public bool IsActive { get; set; }
        public bool IsGroup { get; set; } // True if it's a module with children
        public List<MenuItem> Children { get; set; } = new List<MenuItem>();
        public int Order { get; set; }
    }
}
