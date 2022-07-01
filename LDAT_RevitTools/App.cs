using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

public class App : IExternalApplication
{
    private static string nameApp = "LDAT-Tools";
    private static readonly string pathfileDLL = System.Reflection.Assembly.GetExecutingAssembly().Location;

    public Result OnStartup(UIControlledApplication application)
    {
        CreateMenuApp(application);
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application)
    {
        return Result.Succeeded;
    }
    private void CreateMenuApp(UIControlledApplication uiCA)
    {
        try
        {
            uiCA.CreateRibbonTab(nameApp);
        }
        catch
        {
            return;
        }

        try
        {
            RibbonPanel ribbonPanel = uiCA.CreateRibbonPanel(nameApp, "General");
            try
            {
                PushButtonData pbd = new PushButtonData("SolidRebar", "Solid Rebar", pathfileDLL, nameof(SolidCommand));
                pbd.Image = ConvertIcoToBitmapSource(LDAT_RevitTools.Properties.Resources.RebarSolid);
                pbd.LargeImage = pbd.Image;

                PushButton pb = ribbonPanel.AddItem(pbd) as PushButton;
            }
            catch  { }
        }
        catch 
        { }

    }
    private static BitmapSource ConvertIcoToBitmapSource(System.Drawing.Icon ico)
    {
        try
        {
            Bitmap bitmap = ico.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();
            return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
        catch
        {
            return null;
        }
    }

}

