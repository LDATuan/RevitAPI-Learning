using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using Autodesk.Revit.DB;
using LDATRevitTool.Utilities;

namespace LDATRevitTool.RebarDetailItem.Models;

public class TagInfo
{
    private readonly Document _document;
    private readonly FamilyInstance _detailItem;
    private readonly string _symbolName;
    private readonly Reference _reference;
    private readonly View _view;

    public TagInfo(Document document, FamilyInstance detailItem, string symbolName, Reference reference, View view)
    {
        _document = document;
        _detailItem = detailItem;
        _symbolName = symbolName;
        _reference = reference;
        _view = view;
    }

    private XYZ GetPoint()
    {
        var bb = _detailItem.get_BoundingBox(_view);
        
        var line1 = Line.CreateBound(bb.Min - 100 * _view.RightDirection, bb.Min + 100 * _view.RightDirection);        // var curve1 = _document.Create.NewDetailCurve(_view, line1);
        var line2 = Line.CreateBound(bb.Max + 100 * _view.UpDirection, bb.Max - 100 * _view.UpDirection);

        var setComparisonResult = line1.Intersect(line2, out var intersectionResultArray);

        if (setComparisonResult == SetComparisonResult.Overlap && intersectionResultArray.Size > 0)
        {
            var point = intersectionResultArray.get_Item(0).XYZPoint;

            var midPoint = (bb.Min + point) / 2;
            return midPoint;
        }

        return (bb.Min + bb.Max) / 2;
    }

    public void Insert()
    {
        var point = GetPoint();
        var familySymbol = new FilteredElementCollector(_document).OfClass(typeof(FamilySymbol))
            .FirstOrDefault(f => f.Name == _symbolName) as FamilySymbol;

        IndependentTag.Create(_document, _view.Id, _reference, false, TagMode.TM_ADDBY_CATEGORY,
            TagOrientation.Horizontal, point);
    }
}