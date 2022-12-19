using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LDATRevitTool.Utilities;

namespace LDATRevitTool.RebarDetailItem.Models;

public class TextNoteInfo
{
    private const double OffSet = Setting.Offset;

    private readonly Document _documentFam;
    private readonly View _currentView;
    private readonly RebarStyle _rebarStyle;
    private readonly FamilySymbol _familySymbol;

    public TextNoteInfo(Document documentFam, View view, RebarStyle rebarStyle)
    {
        _documentFam = documentFam;
        _currentView = view;
        _rebarStyle = rebarStyle;
        _familySymbol =
            new FilteredElementCollector(documentFam).OfClass(typeof(FamilySymbol)).FirstElement() as FamilySymbol;
    }

    public void Insert(Line line, double length)
    {
        var direction = line.Direction;
        switch (_rebarStyle)
        {
            case RebarStyle.Standard:
                break;
            case RebarStyle.StirrupTie:
                direction = -line.Direction;
                break;
        }

        var offset = _currentView.Scale/ OffSet;
        var startPoint = line.GetEndPoint(0);
        var endPoint = line.GetEndPoint(1);
        var midPoint = (startPoint + endPoint) / 2;

        var angle = direction.AngleTo(XYZ.BasisX);
        angle = angle.IsEqual(Math.PI) ? 0 : angle;


        if (direction.IsParallelTo(XYZ.BasisX))
        {
            var index = direction.IsSameDirection((XYZ.BasisX)) ? 1 : -1;
            midPoint += index * offset * XYZ.BasisY;
        }
        else if (direction.IsParallelTo(XYZ.BasisY))
        {
            var index = direction.IsSameDirection((XYZ.BasisY)) ? -1 : 1;
            midPoint += index * offset * XYZ.BasisX;
        }
        else
        {
            if (_rebarStyle == RebarStyle.StirrupTie)
            {
                // System.Windows.MessageBox.Show(angle.RadianToDegree().ToString());
                midPoint = startPoint;
                midPoint += (length / 4) * direction - (length / 2) * XYZ.BasisY;

                if (angle.IsLessThan(Math.PI / 2))
                {
                    angle = -angle;
                }
                else
                {
                    angle = angle;
                }
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