using System.Windows;
using System.Windows.Controls;

namespace PacketUtil
{
    /// <summary>
    /// NameValue.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BitCtrl : UserControl
    {
        public BitCtrl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ItemNameProperty =
         DependencyProperty.Register(nameof(ItemName), typeof(string), typeof(BitCtrl),
                         new PropertyMetadata(string.Empty));

        public string? ItemName
        {
            get; set;
        }

        public static readonly DependencyProperty ItemValueProperty =
            DependencyProperty.Register(nameof(ItemValue), typeof(string), typeof(BitCtrl),
                    new PropertyMetadata(string.Empty));

        public string? ItemValue
        {
            get;// { return (string)GetSignedValue(ItemValueProperty); }
            set; //{SetValueHex(ItemValueProperty, value);
        }

  

    }
}
