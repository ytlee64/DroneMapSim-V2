using System.Windows;
using System.Windows.Controls;

namespace DroneMapGCS
{
    public partial class TacticalMap : UserControl
    {
        public TacticalMap()
        {
            InitializeComponent();
        }

        private void MapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is TacticalMapVM vm)
            {
                vm.UpdateCanvasGeometry(e.NewSize.Width, e.NewSize.Height);
            }
        }
    }
}