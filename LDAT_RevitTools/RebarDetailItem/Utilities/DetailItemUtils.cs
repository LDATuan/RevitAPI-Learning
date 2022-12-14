using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LDATRevitTool.RebarDetailItem.Models;
using LDATRevitTool.Utilities;
using Utilities;

namespace LDATRevitTool.RebarDetailItem.Utilities;

public static class DetailItemUtils
{
    public static XYZ PointInsert(this View view, Outline outlineRebar, XYZ pick)
    {
        XYZ point;

        var origin = outlineRebar.MinimumPoint;
        var rightDirection = view.RightDirection;
        var upDirection = view.UpDirection;

        var minRight = outlineRebar.MinimumPoint.DotProduct(rightDirection);
        var maxRight = outlineRebar.MaximumPoint.DotProduct(rightDirection);

        var minUp = outlineRebar.MinimumPoint.DotProduct(upDirection);

        var originUp = origin.DotProduct(upDirection);
        var originRight = origin.DotProduct(rightDirection);


        if (view.UpDirection.IsParallelTo(XYZ.BasisZ))
        {
            if (originRight >= minRight && originRight <= maxRight)
            {
                point = new XYZ(origin.X, pick.Y, pick.Z);
                return point;
            }
            else
            {
                point = new XYZ(pick.X, pick.Y, origin.Z);
                return point;
            }
        }
        else
        {
        }


        return pick;
    }

    public static Family CreateDetailItem(this Application application, Document targetDocument, RebarInfo rebarInfo)
    {
        // const string path =
        //     "D:\\Github\\RevitAPI-Learning\\LDAT_RevitTools\\RebarDetailItem\\Data\\Revit 2020\\Metric Detail Item.rfa";

        const string path =
            "D:\\3. TUAN LE\\01. Github\\RevitAPI-Learning\\LDAT_RevitTools\\RebarDetailItem\\Data\\Revit 2020\\Metric Detail Item.rfa";
        
        var familyDocument = application.OpenDocumentFile(path);

        var factory = familyDocument.FamilyCreate;
        var refLevelView = new FilteredElementCollector(familyDocument).OfClass(typeof(View)).Cast<View>()
            .First(x => x.Name == "Ref. Level");

        var textNoteInfo = new TextNoteInfo(familyDocument, refLevelView);

        using (var transaction = new Transaction(familyDocument))
        {
            transaction.Start("Create Detail Item");

            var index = 0;
            foreach (var curve in rebarInfo.Curves)
            {
                if (curve is Line line)
                {
                    textNoteInfo.Insert(line, rebarInfo.ParameterValues[index]);
                    index++;
                }

                factory.NewDetailCurve(refLevelView, curve);
            }

            transaction.Commit();
        }

        var option = new SaveAsOptions() { OverwriteExistingFile = true, MaximumBackups = 1 };
        var fileName = "RebarId_" + rebarInfo.Rebar.Id + "_ViewId_" + rebarInfo.CurrentView.Id + ".rfa";
        familyDocument.SaveAs(Path.Combine(@"D:\Workspace\Temp\Revit", fileName), option);

        var family = familyDocument.LoadFamily(targetDocument, new LoadFamilyOption());

        familyDocument.Close(false);

        return family;
    }

    public static void Insert(this Document document, Family family, XYZ point)
    {
        if (document.GetElement(family.GetFamilySymbolIds().FirstOrDefault()) is not FamilySymbol familySymbol)
        {
            return;
        }

        using Transaction transaction = new(document);
        transaction.Start("Insert Detail Item");
        if (!familySymbol.IsActive)
        {
            familySymbol.Activate();
        }

        document.Create.NewFamilyInstance(point, familySymbol, document.ActiveView);
        transaction.Commit();
    }
}