using Client.Interfaces.Main;
using DevExpress.Xpf.Editors.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;

namespace Client.Common
{
    /// <summary>
    /// вспомогательные функции для работы с UI WPF
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public class UIUtil
    {
        public UIUtil()
        {
           
        }

        public static T DeepCopy<T>(T element)
        {
            var xaml = XamlWriter.Save(element);
            var xamlString = new StringReader(xaml);
            var xmlTextReader = new XmlTextReader(xamlString);
            var deepCopyObject = (T)XamlReader.Load(xmlTextReader);
            return deepCopyObject;
        }

        public static T GetVisualChild<T>(DependencyObject parent) where T : Visual
        {
            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        /// <summary>
        /// Получаем список всех объектов указанного типа
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static List<T> GetVisualChilds<T>(DependencyObject parent) where T : DependencyObject
        {
            List<T> childs = new List<T>();
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                DependencyObject v = VisualTreeHelper.GetChild(parent, i);
                if (v is T)
                    childs.Add(v as T);
                childs.AddRange(GetVisualChilds<T>(v));
            }
            return childs;
        }

        public static char TagSeparator = ';';
        public static List<string> GetTagList(FrameworkElement frameworkElement)
        {
            List<string> tagList = new List<string>();

            if (frameworkElement != null && frameworkElement.Tag != null)
            {
                string frameworkElementTag = frameworkElement.Tag.ToString();
                if (!string.IsNullOrEmpty(frameworkElementTag))
                {
                    tagList = frameworkElementTag.Split(TagSeparator).ToList();
                }
            }

            return tagList;
        }

        public static void SetFrameworkElementEnabledByTagAccessMode(DependencyObject parent, Role.AccessMode accessMode)
        {
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                DependencyObject frameworkElement = VisualTreeHelper.GetChild(parent, i);
                if (frameworkElement is FrameworkElement)
                {
                    var tagList = UIUtil.GetTagList((FrameworkElement)frameworkElement);
                    var frameworkElementAccessMode = Acl.FindTagAccessMode(tagList);
                    if (frameworkElementAccessMode > accessMode)
                    {
                        ((FrameworkElement)frameworkElement).IsEnabled = false;
                    }
                }
                SetFrameworkElementEnabledByTagAccessMode(frameworkElement, accessMode);
            }
        }



        /// <summary>
        /// Распределение ролей. Проверка прав доступа к элементам управления.
        /// Теги Которые нужно проставлять в элементах управления:
        /// 1) access_mode_full_access
        /// 2) access_mode_read_only
        /// 3) access_mode_special
        /// </summary>
        /// <param name="gridList">Передача списка гридов</param>
        /// <param name="role">Роль</param>
        public static void ProcessPermissions(string role, ContentControl content)
        {
            var mode = Central.Navigator.GetRoleLevel(role);
            var userAccessMode = mode;
            switch (mode)
            {
                case Role.AccessMode.Special:
                    break;

                case Role.AccessMode.FullAccess:
                    break;

                case Role.AccessMode.ReadOnly:
                default:
                    break;
            }

            List<Button> buttons = UIUtil.GetVisualChilds<Button>(content.Content as DependencyObject);
            if (buttons != null && buttons.Count > 0)
            {
                foreach (var button in buttons)
                {
                    var buttonTagList = UIUtil.GetTagList(button);
                    var accessMode = Acl.FindTagAccessMode(buttonTagList);
                    if (accessMode > userAccessMode)
                    {
                        button.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        button.Visibility = Visibility.Visible;
                    }
                }
            }

            List<Menu> menus = UIUtil.GetVisualChilds<Menu>(content.Content as DependencyObject);
            if (menus != null && menus.Count > 0)
            {
                foreach (var menu in menus)
                {
                    if (menu != null)
                    {
                        var menuTagList = UIUtil.GetTagList(menu);
                        var accessMode = Acl.FindTagAccessMode(menuTagList);
                        if (accessMode > userAccessMode)
                        {
                            menu.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            menu.Visibility = Visibility.Visible;
                            ProcessMenuItems(menu.Items, userAccessMode);
                        }
                    }
                }
            }

            List<GridBox> gridList = UIUtil.GetVisualChilds<GridBox>(content.Content as DependencyObject);
            if (gridList != null && gridList.Count > 0)
            {
                foreach (var grid in gridList)
                {
                    if (grid != null && grid.Menu != null)
                    {
                        foreach (var manuItem in grid.Menu)
                        {
                            var manuItemTagList = DataGridContextMenuItem.GetTagList(manuItem.Value);
                            var accessMode = Acl.FindTagAccessMode(manuItemTagList);
                            if (accessMode > userAccessMode)
                            {
                                manuItem.Value.Visible = false;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Рекурсивно обрабатsвает MenuiTem и их вложенные элементы, устанавливая видимость в зависимости от режима доступа.
        /// </summary>
        /// <param name="items">Коллекция Items в Menu или MenuItem</param>
        /// <param name="userAccessMode">Режим доступа пользователя</param>
        private static void ProcessMenuItems(ItemCollection items, Role.AccessMode userAccessMode)
        {
            foreach (var item in items)
            {
                if (item is MenuItem menuItem)
                {
                    var menuItemTagList = UIUtil.GetTagList(menuItem);
                    var accessMode = Acl.FindTagAccessMode(menuItemTagList);

                    if (accessMode > userAccessMode)
                    {
                        menuItem.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        menuItem.Visibility = Visibility.Visible;
                    }

                    if (menuItem.HasItems)
                    {
                        ProcessMenuItems(menuItem.Items, userAccessMode);
                    }
                }
            }
        }

        public static ScrollViewer GetScrollViewer(UIElement element)
        {
            if (element == null) return null;

            ScrollViewer retour = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element) && retour == null; i++) {
                if (VisualTreeHelper.GetChild(element, i) is ScrollViewer) {
                    retour = (ScrollViewer) (VisualTreeHelper.GetChild(element, i));
                }
                else {
                    retour = GetScrollViewer(VisualTreeHelper.GetChild(element, i) as UIElement);
                }
            }
            return retour;
        }

        /// <summary>
        /// изменить на единицу значение указанного поля
        /// action 1=increase 2=decrease
        /// </summary>
        /// <param name="ctl"></param>
        /// <param name="action"></param>
        public static void ChangeIntValue(Control ctl, int action=1, int min=1, int max=14)
        {
            if(ctl!=null)
            {
                var textBox=(TextBox)ctl;
                var i=textBox.Text.ToInt();
                switch(action)
                {
                    //inc
                    case 1:
                        i++;
                        break;

                    //dec
                    case 2:
                        i--;
                        break;
                }

                if(i<min)
                {
                    i=min;
                }

                if(i>max)
                {
                    i=max;
                }


                textBox.Text=i.ToString();
            }
        }

        public static bool IsUserVisible(UIElement element)
        {
            if (!element.IsVisible)
                return false;
            var container = VisualTreeHelper.GetParent(element) as FrameworkElement;
            if (container == null) throw new ArgumentNullException("container");

            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.RenderSize.Width, element.RenderSize.Height));
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.IntersectsWith(bounds);
        }
    }
}
