using PacketUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace PacketUtil
{
    /// <summary>
    /// DlgWord.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DlgWord : Window
    {

        public DlgWord(PacketUtil.PacketItem item,bool Readonly)
        {
            InitializeComponent();
            DataContext = new DlgWordVM(item);
            BitList.IsReadOnly = Readonly;
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ((DlgWordVM)DataContext).Update();
            DialogResult = true;
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
