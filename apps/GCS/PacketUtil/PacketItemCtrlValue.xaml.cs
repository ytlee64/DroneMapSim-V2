using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PacketUtil
{
    /// <summary>
    /// NameStatusCtrl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PacketItemCtrlValue: UserControl
    {
        public PacketItemCtrlValue()
        {
            InitializeComponent();          
        }

        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender == null)
                return;

            TextBox? textBox = sender as TextBox;
            if (textBox != null)
            {
                // 바인딩된 데이터에서 상세 정보를 가져옵니다.
                var dataContext = textBox.DataContext as PacketItem; // YourDataModel을 실제 데이터 모델 클래스로 대체하세요.
                if (dataContext != null)
                {
                    // 상세 정보를 표시하는 로직을 여기에 추가합니다.
                    //MessageBox.Show($"Name: {dataContext.Name}\nSymbol: {dataContext.Symbol}", "Detailed Information", MessageBoxButton.OK, MessageBoxImage.Information);

                    PacketItem item = (PacketItem)dataContext;
                    if (item.BitList.Length > 0)
                    {
                        DlgWord dlg = new(item, false);
                        Console.WriteLine($"Column clicked: {item.Name} {item.Value}");
                        if (item.Value.StartsWith("0x"))
                        {
                            dlg.Owner = System.Windows.Application.Current.MainWindow;
                            dlg.ShowDialog();
                        }
                    }

                }
            }
        }
    }


}

