using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PacketUtil
{
    public static class DataGridUtils
    {
        public static void MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var selectedItem = dataGrid.SelectedItem;
                if (selectedItem != null)
                {
                    var viewModel = dataGrid.DataContext as BaseVM;
                    if (viewModel != null)
                    {
                        viewModel.OnRowSelect(selectedItem);
                    }
                }
            }
        }


        public static void BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            var column = e.Column as DataGridTextColumn;
            var row = e.Row.Item as PacketUtil.PacketItem;

            if (column!=null && column.Header.ToString() == "Value" && row != null)
            {
                string value = row.Value.ToString();

                // "0x"로 시작하면 편집을 막습니다.
                if (value.StartsWith("Bit"))
                {
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = true;
            }
        }
    }
}
