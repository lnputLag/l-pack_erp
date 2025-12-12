using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Interfaces.Main
{
    /// <summary>
    /// Класс для локализации сообщений грида
    /// https://docs.devexpress.com/WPF/DevExpress.Xpf.Grid.GridControlLocalizer
    /// </summary>
    /// <author>eletskikh_ya</author>

    public class GridBox4Localizer : GridControlLocalizer
    {
        public static bool EnableLocalize = false;

        static GridBox4Localizer()
        {
            GridControlLocalizer.Active = new GridBox4Localizer();
        }

        protected override void PopulateStringTable()
        {
            base.PopulateStringTable();

            AddString(GridControlStringId.ExcelColumnFilterPopupFilterRulesTabCaption, "Правила фильтра");
            AddString(GridControlStringId.ExcelColumnFilterPopupClearFilter, "Сброс фильтра");
            AddString(GridControlStringId.MenuColumnClearFilter, "Сброс фильтра");

            AddString(GridControlStringId.MenuColumnConditionalFormatting_ClearRules, "Сброс правил");
            
            AddString(GridControlStringId.PopupFilterAll, "Все");

            AddString(GridControlStringId.DDExtensionsDraggingMultipleRows, "Перенос строк");
            AddString(GridControlStringId.DDExtensionsDraggingOneRow, "Перенос строки");

            AddString(GridControlStringId.BandChooserDragText, "Перенос строки1");
            AddString(GridControlStringId.ColumnChooserDragText, "Перенос строки2");

            AddString(GridControlStringId.ExcelColumnFilterPopupSearchNullTextAll, "Все");
            AddString(GridControlStringId.ExcelColumnFilterPopupSearchNullText, "Поиск");

            AddString(GridControlStringId.CheckboxSelectorColumnCaption, "Включено");
            //AddString(GridControlStringId.Unch, "Включено");

            //AddString(GridControlStringId., "Поиск");


            //AddString(GridControlStringId., "Поиск");




            // Changes the caption of the menu item used to invoke the Total Summary Editor.
            AddString(GridControlStringId.MenuFooterCustomize, "Customize Totals");

            // Changes the Total Summary Editor's default caption.
            AddString(GridControlStringId.TotalSummaryEditorFormCaption, "Totals Editor");

            // Changes the default caption of the tab page that lists total summary items.
            AddString(GridControlStringId.SummaryEditorFormItemsTabCaption, "Summary Items");
        }
    }
}
