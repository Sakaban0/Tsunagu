namespace AiPromptHelper.Core;

public static class Appearance
{
    public static double PointsToDip(double points) => points * 96d / 72d;
    public static (byte R,byte G,byte B) HsvToRgb(double hue,double saturation,double value)
    {
        hue=((hue%360)+360)%360; saturation=Math.Clamp(saturation,0,1);value=Math.Clamp(value,0,1);
        double c=value*saturation,x=c*(1-Math.Abs(hue/60%2-1)),m=value-c;
        (double r,double g,double b)=hue switch {<60=>(c,x,0d),<120=>(x,c,0d),<180=>(0d,c,x),<240=>(0d,x,c),<300=>(x,0d,c),_=>(c,0d,x)};
        return ((byte)Math.Round((r+m)*255),(byte)Math.Round((g+m)*255),(byte)Math.Round((b+m)*255));
    }
    public static (double H,double S,double V) RgbToHsv(byte r,byte g,byte b)
    {
        double red=r/255d,green=g/255d,blue=b/255d,max=Math.Max(red,Math.Max(green,blue)),min=Math.Min(red,Math.Min(green,blue)),delta=max-min;
        double hue=delta==0?0:max==red?60*((green-blue)/delta%6):max==green?60*((blue-red)/delta+2):60*((red-green)/delta+4);
        return ((hue+360)%360,max==0?0:delta/max,max);
    }
    public static double Luminance(byte r,byte g,byte b)
    { double Channel(byte c){double x=c/255d;return x<=.04045?x/12.92:Math.Pow((x+.055)/1.055,2.4);}return .2126*Channel(r)+.7152*Channel(g)+.0722*Channel(b); }
    // Choosing pure black or white guarantees >= 4.5:1 for any solid RGB background.
    public static string ContrastText(byte r,byte g,byte b)=>Luminance(r,g,b)>.179?"#000000":"#FFFFFF";
}
