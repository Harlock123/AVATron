using Avalonia.Controls;
using Avalonia.Media;

namespace Robotron.Avalonia;

public sealed class MainWindow : Window
{
    public MainWindow()
    {
        Title = "Robotron spike";
        Width = 960; Height = 720;
        Background = Brushes.Black;
        Content = new SpikeView();
    }
}
