using System.Windows;
using System.Windows.Media;
namespace AiPromptHelper.Views;
public static class CardIcon
{
    public static FrameworkElement Create(string id)
    {
        string path=id switch
        {
            "ideas"=>"M 8,18 L 16,18 M 9,21 L 15,21 M 8,15 C 8,12 5,12 5,8 C 5,0 19,0 19,8 C 19,12 16,12 16,15 Z",
            "study"=>"M 12,5 L 12,21 M 12,5 L 3,3 L 3,18 L 12,21 L 21,18 L 21,3 Z",
            "pc-help" or "pc-terms"=>"M 3,3 L 21,3 L 21,16 L 3,16 Z M 12,16 L 12,21 M 8,21 L 16,21",
            "writing"=>"M 4,20 L 5,14 L 17,2 L 22,7 L 10,19 Z M 14,5 L 19,10",
            "summary"=>"M 5,2 L 19,2 L 19,22 L 5,22 Z M 8,7 L 16,7 M 8,12 L 16,12 M 8,17 L 13,17",
            _=>"M 5,18 C -2,10 4,2 12,2 C 23,2 26,18 15,21 C 11,22 8,20 7,20 L 3,22 Z"
        };
        var icon=new System.Windows.Shapes.Path{Data=Geometry.Parse(path),StrokeThickness=1.5,Fill=Brushes.Transparent,Width=24,Height=24,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,8)};
        icon.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty,"MutedBrush");return icon;
    }
}
