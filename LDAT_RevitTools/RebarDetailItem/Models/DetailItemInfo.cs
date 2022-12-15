using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using LDATRevitTool.Utilities;

namespace LDATRevitTool.RebarDetailItem.Models;

public class DetailItemInfo
{
    private readonly UIApplication _uiApplication;
    private readonly RebarInfo _rebarInfo;
    private readonly Document _document;
    private readonly string _name;

    private Family _familyInProject;
    private bool IsFamilyInProject { get; set; }

    public DetailItemInfo(UIApplication uiApplication, RebarInfo rebarInfo)
    {
        _uiApplication = uiApplication;
        _rebarInfo = rebarInfo;
        _document = rebarInfo.Rebar.Document;
        _name = "RebarId_" + rebarInfo.Rebar.Id + "_viewId_" + rebarInfo.CurrentView.Id;
        IsFamilyInProject = CheckFamilyInProject();
    }

    private bool CheckFamilyInProject()
    {
        var element = new FilteredElementCollector(_document).OfClass(typeof(Family))
            .FirstOrDefault(f => f.Name == _name);
        if (element is null)
        {
            return false;
        }
        else
        {
            _familyInProject = (Family)element;
            return true;
        }
    }

    private Document GetFamilyDocument()
    {
        Document familyDocument;
        if (this.IsFamilyInProject)
        {
            familyDocument = _document.EditFamily(_familyInProject);
            var categoryFilter = new ElementMulticategoryFilter(
                new List<BuiltInCategory>() { BuiltInCategory.OST_Lines, BuiltInCategory.OST_GenericAnnotation });
            var collector = new FilteredElementCollector(familyDocument).WhereElementIsNotElementType()
                .WherePasses(categoryFilter).ToElementIds();
            using var transaction = new Transaction(familyDocument);
            transaction.Start("Clean");
            familyDocument.Delete(collector);
            transaction.Commit();
        }
        else
        {
            // const string path =
            // 	"D:\\Github\\RevitAPI-Learning\\LDAT_RevitTools\\RebarDetailItem\\Data\\Revit 2020\\Metric Detail Item.rfa" ;

            const string path =
                "D:\\3. TUAN LE\\01. Github\\RevitAPI-Learning\\LDAT_RevitTools\\RebarDetailItem\\Data\\Revit 2020\\Metric Detail Item.rfa";


            familyDocument = _uiApplication.Application.OpenDocumentFile(path);
        }

        return familyDocument;
    }

    private XYZ PointInsert(XYZ pickPoint)
    {
        var outlineRebar = _rebarInfo.Outline;
        var view = _rebarInfo.CurrentView;

        var origin = outlineRebar.MinimumPoint;
        var rightDirection = view.RightDirection;
        // var upDirection = view.UpDirection;

        var minRight = outlineRebar.MinimumPoint.DotProduct(rightDirection);
        var maxRight = outlineRebar.MaximumPoint.DotProduct(rightDirection);

        // var minUp = outlineRebar.MinimumPoint.DotProduct(upDirection);
        //
        // var originUp = pickPoint.DotProduct(upDirection);
        var pickPointRight = pickPoint.DotProduct(rightDirection);

        XYZ point;

        if (view.UpDirection.IsParallelTo(XYZ.BasisZ))
        {
            if (pickPointRight >= minRight && pickPointRight <= maxRight)
            {
                point = new XYZ(origin.X, origin.Y, pickPoint.Z);
                return point;
            }

            point = new XYZ(pickPoint.X, pickPoint.Y, origin.Z);
            return point;
        }

        if (view.ViewDirection.IsParallelTo(XYZ.BasisZ))
        {
            if (pickPointRight >= minRight && pickPointRight <= maxRight)
            {
                point = new XYZ(origin.X, pickPoint.Y, pickPoint.Z);
                return point;
            }

            point = new XYZ(pickPoint.X, origin.Y, pickPoint.Z);
            return point;
        }


        return pickPoint;
    }

    private Family CreateDetailFamily()
    {
        var familyDocument = this.GetFamilyDocument();
        var factory = familyDocument.FamilyCreate;
        var refLevelView = new FilteredElementCollector(familyDocument).OfClass(typeof(View)).Cast<View>()
            .First(x => x.Name == "Ref. Level");

        var textNoteInfo = new TextNoteInfo(familyDocument, refLevelView);

        using (var transaction = new Transaction(familyDocument))
        {
            transaction.Start("Create Detail Item");

            var index = 0;
            var isSameViewDirection = _rebarInfo.CurrentView.ViewDirection.IsSameDirection(_rebarInfo.Normal);
            foreach (var curve in _rebarInfo.Curves)
            {
                if (curve is Line line)
                {
                    if (_rebarInfo.RebarStyle == RebarStyle.Standard)
                    {
                        textNoteInfo.Insert(line, _rebarInfo.ParameterValues[index]);
                        index++;
                    }
                    else
                    {
                        if (!(_rebarInfo.ParameterValues.Count == 6 &&
                              index == 5))
                        {
                            textNoteInfo.Insert(line, _rebarInfo.ParameterValues[index], isSameViewDirection);
                            index++;
                        }
                    }
                }

                factory.NewDetailCurve(refLevelView, curve);
            }

            transaction.Commit();
        }

        var option = new SaveAsOptions() { OverwriteExistingFile = true, MaximumBackups = 1 };
        var fileName = this._name + ".rfa";
        familyDocument.SaveAs(Path.Combine(@"D:\Workspace\Temp\Revit", fileName), option);

        var family = familyDocument.LoadFamily(_document, new LoadFamilyOption());

        familyDocument.Close(false);

        return family;
    }

    public void Insert()
    {
        var uiDocument = _uiApplication.ActiveUIDocument;
        var document = uiDocument.Document;

        var family = CreateDetailFamily();

        if (document.GetElement(family.GetFamilySymbolIds().FirstOrDefault()) is not FamilySymbol familySymbol)
        {
            return;
        }

        using Transaction transaction = new(document);
        transaction.Start("Insert Detail Item");

        var view = document.ActiveView;
        var plane = Plane.CreateByNormalAndOrigin(view.ViewDirection, view.Origin);
        var sketchPlane = SketchPlane.Create(document, plane);
        document.ActiveView.SketchPlane = sketchPlane;

        var pickPoint = uiDocument.Selection.PickPoint();

        if (!familySymbol.IsActive)
        {
            familySymbol.Activate();
        }

        var point = PointInsert(pickPoint);

        document.Create.NewFamilyInstance(point, familySymbol, view);

        transaction.Commit();
    }
}