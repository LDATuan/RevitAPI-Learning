using System;
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
    private const string RebarShapeName = "T_RebarShape2D_";

    private readonly UIApplication _uiApplication;
    private readonly RebarShapeInfo _rebarShapeInfo;
    private readonly Document _document;
    private readonly string _name;

    private Family _familyInProject;
    private bool IsFamilyInProject { get; set; }

    public DetailItemInfo(UIApplication uiApplication, RebarShapeInfo rebarShapeInfo)
    {
        _uiApplication = uiApplication;
        _rebarShapeInfo = rebarShapeInfo;
        _document = rebarShapeInfo.Rebar.Document;
        var shape2DId = rebarShapeInfo.Rebar.Id.IntegerValue + rebarShapeInfo.CurrentView.Id.IntegerValue;
        _name = RebarShapeName + shape2DId;
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

        _familyInProject = (Family)element;
        return true;
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
            const string path =
                "D:\\Github\\RevitAPI-Learning\\LDAT_RevitTools\\RebarDetailItem\\Data\\Revit 2020\\Metric Detail Item.rfa";

            // const string path =
            //     "D:\\3. TUAN LE\\01. Github\\RevitAPI-Learning\\LDAT_RevitTools\\RebarDetailItem\\Data\\Revit 2020\\Metric Detail Item.rfa";


            familyDocument = _uiApplication.Application.OpenDocumentFile(path);
        }

        return familyDocument;
    }

    private XYZ PointInsert(XYZ pickPoint)
    {
        var outlineRebar = _rebarShapeInfo.Outline;
        var view = _rebarShapeInfo.CurrentView;

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

        var textNoteInfo = new TextNoteInfo(familyDocument, refLevelView, _rebarShapeInfo.RebarStyle);

        using (var transaction = new Transaction(familyDocument))
        {
            transaction.Start("Create Detail Item");

            var index = 0;
            foreach (var curve in _rebarShapeInfo.Curves)
            {
                if (curve is Line line)
                {
                    if (_rebarShapeInfo.RebarStyle == RebarStyle.Standard)
                    {
                        textNoteInfo.Insert(line, _rebarShapeInfo.ParameterValues[index]);
                        index++;
                    }
                    else
                    {
                        if (!(_rebarShapeInfo.ParameterValues.Count == 6 &&
                              index == 5))
                        {
                            textNoteInfo.Insert(line, _rebarShapeInfo.ParameterValues[index]);
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

        var tempFolder = Path.GetTempPath();
        var detailItemFolderName = "Detail Item Folder";
        var detailFolder = Path.Combine(tempFolder, detailItemFolderName);
        if (!Directory.Exists(detailFolder))
        {
            Directory.CreateDirectory(detailFolder);
        }

        familyDocument.SaveAs(Path.Combine(detailFolder, fileName), option);

        var family = familyDocument.LoadFamily(_document, new LoadFamilyOption());

        familyDocument.Close(false);

        return family;
    }

    public bool Insert()
    {
        var uiDocument = _uiApplication.ActiveUIDocument;
        var document = uiDocument.Document;

        var family = CreateDetailFamily();

        if (document.GetElement(family.GetFamilySymbolIds().FirstOrDefault()) is not FamilySymbol familySymbol)
        {
            return false;
        }

        using (var transactionGroup = new TransactionGroup(_document))
        {
            transactionGroup.Start("Detail Iterm");

            FamilyInstance detailItem = null;
            using (Transaction transaction = new(document))
            {
                transaction.Start("Insert Detail Item");

                var view = document.ActiveView;
                var plane = Plane.CreateByNormalAndOrigin(view.ViewDirection, view.Origin);
                if (document.ActiveView.SketchPlane == null)
                {
                    var sketchPlane = SketchPlane.Create(document, plane);
                    document.ActiveView.SketchPlane = sketchPlane;
                }

                XYZ pickPoint;
                try
                {
                    pickPoint = uiDocument.Selection.PickPoint();
                }
                catch
                {
                    return false;
                }

                if (!familySymbol.IsActive)
                {
                    familySymbol.Activate();
                }

                var point = PointInsert(pickPoint);

                detailItem = document.Create.NewFamilyInstance(point, familySymbol, view);
                transaction.Commit();
            }

            if (detailItem == null)
            {
                return false;
            }

            using (var transaction = new Transaction(_document))
            {
                transaction.Start("Insert tag");

                var tagInfo = new TagInfo(_document, detailItem, "Type & Number", _rebarShapeInfo.Reference,
                    _rebarShapeInfo.CurrentView);
                tagInfo.Insert();
                transaction.Commit();
            }

            transactionGroup.Assimilate();
        }

        return true;
    }
}