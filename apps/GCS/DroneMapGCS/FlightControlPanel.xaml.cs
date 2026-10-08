using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DroneMapGCS
{
    public partial class FlightControlPanel : UserControl
    {
        public FlightControlPanel()
        {
            InitializeComponent();
        }

        // 단발성 명령 버튼 (1, I, 방향키, C, V) 클릭 시 HandleKeyDown 호출
        private void OnKeyButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string keyName &&
                Enum.TryParse<Key>(keyName, out Key key) &&
                DataContext is MainWindowVM vm)
            {
                vm.HandleKeyDown(key);
            }
        }

        // 연속 비행 조종 버튼 (W, A, S, D, Q, E) 누를 때 HandleKeyDown 호출
        private void OnFlightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string keyName &&
                Enum.TryParse<Key>(keyName, out Key key) &&
                DataContext is MainWindowVM vm)
            {
                vm.HandleKeyDown(key);
            }
        }

        // 연속 비행 조종 버튼 (W, A, S, D, Q, E) 뗄 때 HandleKeyUp 호출 (중립 복귀)
        private void OnFlightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string keyName &&
                Enum.TryParse<Key>(keyName, out Key key) &&
                DataContext is MainWindowVM vm)
            {
                vm.HandleKeyUp(key);
            }
        }
    }
}