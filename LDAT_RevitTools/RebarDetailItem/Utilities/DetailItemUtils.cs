using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using LDATRevitTool.RebarDetailItem.Models;
using LDATRevitTool.Utilities;
using Utilities;

namespace LDATRevitTool.RebarDetailItem.Utilities;

public static class DetailItemUtils
{
    private static XYZ PointInsert(this View view, Outline outlineRebar, XYZ pickPoint)
    {
        var origin = outlineRebar.MinimumPoint;
        var rightDirection = view.RightDirection;
        var upDirection = view.UpDirection;

        var minRight = outlineRebar.MinimumPoint.DotProduct(rightDirection);
        var maxRight = outlineRebar.MaximumPoint.DotProduct(rightDirection);

        var minUp = outlineRebar.MinimumPoint.DotProduct(upDirection);

        var originUp = pickPoint.DotProduct(upDirection);
        var pickPointRight = pickPoint.DotProduct(rightDirection);
        
        XYZ point;
        
        if (view.UpDirection.IsParallelTo(XYZ.BasisZ)) {
            if (pickPointRight >= minRight && pickPointRight <= maxRight) {
                point = new XYZ(origin.X, origin.Y, pickPoint.Z);
                return point;
            }

            point = new XYZ(pickPoint.X, pickPoint.Y, origin.Z);
            return point;
        }

        if (view.ViewDirection.IsParallelTo(XYZ.BasisZ)) {
            if (pickPointRight >= minRight && pickPointRight <= maxRight) {
                point = new XYZ(origin.X, pickPoint.Y, pickPoint.Z);
                return point;
            }

            point = new XYZ(pickPoint.X, origin.Y, pickPoint.Z);
            return point;
        }


        return pickPoint;
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

        using (var transaction = new Transaction(familyDocument)) {
            transaction.Start("Create Detail Item");

            var index = 0;
            foreach (var curve in rebarInfo.Curves) {
                if (curve is Line line) {
                    textNoteInfo.Insert(line, rebarInfo.ParameterValues[ index ]);
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

    public static void Insert(this UIDocument uiDocument, Family family, Outline outline)
    {
        var document = uiDocument.Document;
        if (document.GetElement(family.GetFamilySymbolIds().FirstOrDefault()) is not FamilySymbol familySymbol) {
            return;
        }

        using Transaction transaction = new(document);
        transaction.Start("Insert Detail Item");

        var view = document.ActiveView;
        var plane = Plane.CreateByNormalAndOrigin(view.ViewDirection, view.Origin);
        var sketchPlane = SketchPlane.Create(document, plane);
        document.ActiveView.SketchPlane = sketchPlane;

        var pickPoint = uiDocument.Selection.PickPoint();

        if (!familySymbol.IsActive) {
            familySymbol.Activate();
        }

        var point = view.PointInsert(outline, pickPoint);

        document.Create.NewFamilyInstance(point, familySymbol, view);

        transaction.Commit();
    }
}