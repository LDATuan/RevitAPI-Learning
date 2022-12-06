using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LDATRevitTool.Utilities;
using Utilities;

namespace RebarDetailItem.Utilities;

public static class DetailItemUtils
{

    public static XYZ PointInsert(this View view, XYZ origin, XYZ pick)
    {


        XYZ point;
        if (view.RightDirection.IsParallelTo(XYZ.BasisZ))
        {
            point = new XYZ(origin.X, origin.Y, pick.Z);
        }
        else
        {
            point = new XYZ(origin.X, origin.Y, pick.Z);
        }
        return point;
    }

    public static Family CreateDetailItem(this Application application, Document targetDocument, List<Curve> curves, int idRebar, int IdView)
    {
        var path = "C:\\ProgramData\\Autodesk\\RVT 2020\\Family Templates\\English\\Metric Detail Item.rft";

        var familyDocument = application.NewFamilyDocument(path);

        Autodesk.Revit.Creation.FamilyItemFactory factory = familyDocument.FamilyCreate;
        View famView = new FilteredElementCollector(familyDocument).OfClass(typeof(View)).Cast<View>()
          .First(x => x.Name == "Ref. Level");

        using (Transaction transaction = new Transaction(familyDocument))
        {
            transaction.Start("Create Detail Item");

            foreach (var curve in curves)
            {
                factory.NewDetailCurve(famView, curve);
            }

            transaction.Commit();
        }

        var option = new SaveAsOptions() { OverwriteExistingFile = true, MaximumBackups = 1 };
        var fileName = "RebarId_" + idRebar + "_ViewId_" + IdView + ".rfa";
        familyDocument.SaveAs(Path.Combine(@"D:\Workspace\Temp\Revit", fileName), option);

        var family = familyDocument.LoadFamily(targetDocument, new LoadFamilyOption());

        familyDocument.Close(false);

        return family;
    }

    public static void Insert(this Document document, Family family, XYZ point)
    {
        using (Transaction transaction = new(document))
        {
            transaction.Start("Insert Detail Item");
            var familySymbol = document.GetElement(family.GetFamilySymbolIds().First()) as FamilySymbol;
            if (!familySymbol.IsActive)
            {
                familySymbol.Activate();
            }
            document.Create.NewFamilyInstance(point, familySymbol, document.ActiveView);
            transaction.Commit();
        }
    }
}


