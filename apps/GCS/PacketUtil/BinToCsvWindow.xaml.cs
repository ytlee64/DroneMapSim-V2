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
using System.Windows.Forms;
using System.Reflection.Emit;
using System.Collections.ObjectModel;

namespace PacketUtil
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class BinToCsvWindow : Window
    {
        
        public BinToCsvWindow()
        {
            InitializeComponent();

            this.DataContext = BinToCsvVM.GetInstance();
        }

        public void Init(ObservableCollection<PacketUtil.Packet> ?packets)
        {
            BinToCsvVM.GetInstance().Init(packets);
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                // Show the folder browser dialog
                DialogResult result = dialog.ShowDialog();

                // If a folder is selected, update the TextBox with the folder path
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    FolderPathTextBox.Text = dialog.SelectedPath;

                    BinToCsvVM.GetInstance().CsvConvertStart(dialog.SelectedPath);
                }
            }
        }
    }
}
