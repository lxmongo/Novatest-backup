using System.Collections.Generic;

namespace nanovaTest
{
    /// <summary>
    /// the homepage mainmenu entity
    /// </summary>
    public class MainMenu
    {
        public int ID { get; set; }
        public string Image { get; set; }
        public string MenuName { get; set; }
    }

    /// <summary>
    /// initialization MainMenu with icon and name
    /// </summary>
    public class MainMenuManger
    {
        public static List<MainMenu> GetMainMenus()
        {
            var menus = new List<MainMenu>();
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            menus.Add(new MainMenu { ID = 1, Image = "Assets/select-method-icon.png", MenuName = loader.GetString("MainMenuSelectMethod") });
            menus.Add(new MainMenu { ID = 2, Image = "Assets/custom-method-icon.png", MenuName = loader.GetString("MainMenuCustomMethod") });

            menus.Add(new MainMenu { ID = 3, Image = "Assets/method-history-icon.png", MenuName = loader.GetString("MainMenuMethodHistory") });
            menus.Add(new MainMenu { ID = 4, Image = "Assets/calibrate-icon.png", MenuName = loader.GetString("MainMenuCalibrate") });
            menus.Add(new MainMenu { ID = 5, Image = "Assets/voc-library-icon.png", MenuName = loader.GetString("MainMenuVOCLibrary") });
            return menus;
        }
    }
}
