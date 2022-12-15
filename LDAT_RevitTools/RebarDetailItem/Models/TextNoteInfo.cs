using System;
using System.Diagnostics;
using Autodesk.Revit.DB;
using LDATRevitTool.Utilities;

namespace LDATRevitTool.RebarDetailItem.Models;

public class TextNoteInfo
{
    private const double OffSet = Setting.Offset;

    private readonly Document _documentFam;
    private readonly View _currentView;
    private readonly FamilySymbol _familySymbol;

    public TextNoteInfo(Document documentFam, View view)
    {
        _documentFam = documentFam;
        _currentView = view;
        _familySymbol =
            new FilteredElementCollector(documentFam).OfClass(typeof(FamilySymbol)).FirstElement() as FamilySymbol;
    }

    public void Insert(Line line, double length, bool isSameViewDirection = true)
    {
        var direction = line.Direction;

        var offset = OffSet.Millimeter2Feet();
        var startPoint = line.GetEndPoint(0);
        var endPoint = line.GetEndPoint(1);
        var midPoint = (startPoint + endPoint) / 2;

        var angle = direction.AngleTo(XYZ.BasisX);
        angle = angle.IsEqual(Math.PI) ? 0 : angle;

        if (direction.IsParallelTo(XYZ.BasisX))
        {
            var index = direction.IsSameDirection((XYZ.BasisX)) && !isSameViewDirection ? 1 : -1;
            midPoint += index * offset * XYZ.BasisY;
        }
        else if (direction.IsParallelTo(XYZ.BasisY))
        {
            var index = direction.IsSameDirection((XYZ.BasisY)) && isSameViewDirection ? -1 : 1;
            midPoint += index * offset * XYZ.BasisX;
        }
        else
        {
            if (startPoint.Y.IsGreaterThan(endPoint.Y))
            {
                angle = -angle;
                midPoint += offset * XYZ.BasisX + offset * XYZ.BasisY;
            }
            else
            {
                midPoint += -offset * XYZ.BasisX + offset * XYZ.BasisY;
            }
        }

        var textNote = _documentFam.FamilyCreate.NewFamilyInstance(midPoint, _familySymbol, _currentView);
        textNote.LookupParameter("Length").Set(length);

        if (angle != 0)
        {
            var axis = Line.CreateBound(midPoint, midPoint + XYZ.BasisZ);
            ElementTransformUtils.RotateElement(_documentFam, textNote.Id, axis, angle);
        }
    }
}