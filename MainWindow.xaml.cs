using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ACGCET_Faculty
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ShowF11Hint();
        }

        private void ShowF11Hint()
        {
            var hint = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(220, 30, 30, 30)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(18, 10, 18, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 12, 0, 0),
                IsHitTestVisible = false
            };
            var txt = new TextBlock
            {
                Text = "App opened in maximized mode  \u2022  Press F11 to toggle fullscreen",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold
            };
            hint.Child = txt;

            if (Content is Grid grid)
            {
                grid.Children.Add(hint);
                Panel.SetZIndex(hint, 9999);
                var timer = new DispatcherTimer { Interval = System.TimeSpan.FromSeconds(4) };
                timer.Tick += (s, _) => { timer.Stop(); grid.Children.Remove(hint); };
                timer.Start();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                if (WindowStyle == WindowStyle.None)
                {
                    WindowStyle = WindowStyle.SingleBorderWindow;
                    WindowState = WindowState.Normal;
                    ResizeMode = ResizeMode.CanResize;
                }
                else
                {
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Maximized;
                    ResizeMode = ResizeMode.NoResize;
                }
                e.Handled = true;
            }

            if (e.Key == Key.Escape && WindowStyle == WindowStyle.None)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
                ResizeMode = ResizeMode.CanResize;
                e.Handled = true;
            }
        }
    }
}
