using System.Windows;
using System.Windows.Input;

namespace DroneMapGCS
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // 키보드 키를 누를 때 뷰모델로 전달
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (DataContext is MainWindowVM vm)
            {
                vm.HandleKeyDown(e.Key);
            }
        }

        // 캔버스 창 크기가 변할 때 ViewModel에 크기 전달
        private void MapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainWindowVM vm)
            {
                vm.UpdateCanvasGeometry(e.NewSize.Width, e.NewSize.Height);
            }
        }

    }
}