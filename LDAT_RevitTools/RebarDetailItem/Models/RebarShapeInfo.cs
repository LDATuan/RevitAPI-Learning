using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LDATRevitTool.RebarDetailItem.Utilities;
using LDATRevitTool.Utilities;

namespace LDATRevitTool.RebarDetailItem.Models;

public class RebarShapeInfo
{
    private readonly IEnumerable<Curve> _curves;
    public View CurrentView { get; }

    public Rebar Rebar { get; }

    public Reference Reference { get;  }
    public RebarStyle RebarStyle { get; }

    public XYZ Normal { get; }

    public XYZ RightDirection { get; set; }

    public Outline Outline { get; }

    public List<Curve> Curves { get; }

    public List<double> ParameterValues { get; }

    public RebarShapeInfo(Rebar rebar)
    {
        _curves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0);

        CurrentView = rebar.Document.ActiveView;
        this.Rebar = rebar;
        this.Reference = new Reference(rebar);
        this.RebarStyle = (RebarStyle)rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_HOOK_STYLE).AsInteger();
        this.Normal = rebar.GetShapeDrivenAccessor().Normal;
        this.ParameterValues = rebar.GetParameterValues();
        this.Curves = _curves.ToList();
        this.Outline = this.GetOutLineRebar();

        this.FindRightDirection();
        this.TransformCurves();
    }

    private void FindRightDirection()
    {
        if (this.Normal.IsParallelTo(XYZ.BasisZ))
        {
            this.RightDirection = this.Normal.IsSameDirection(XYZ.BasisZ) ? XYZ.BasisX : -XYZ.BasisX;
        }
        else
        {
            this.RightDirection = XYZ.BasisZ.CrossProduct(this.Normal);
        }
    }

    private Transform CreateTranslation()
    {
        var transform = Transform.Identity;
        var outline = this.GetOutLineRebar();

        transform = Transform.CreateTranslation(XYZ.Zero - outline.MinimumPoint) * transform;

        return transform;
    }

    private static double FindAngle(XYZ vector1, XYZ vector2)
    {
        var dotAngle = vector1.DotProduct(vector2);
        var angle = vector1.AngleTo(vector2);
        return angle;
    }

    private Transform CreateRotation()
    {
        var transform = Transform.Identity;
        var vectorRight = XYZ.BasisZ.CrossProduct(this.Normal);
        var isSameViewDirection = this.CurrentView.ViewDirection.IsSameDirection(this.Normal);


        var angleZ =
            FindAngle(
                this.CurrentView.ViewDirection.IsParallelTo(this.Normal) ? this.CurrentView.ViewDirection : Normal,
                XYZ.BasisZ);

        if (angleZ != 0 && vectorRight.DotProduct(XYZ.BasisY) != 0)
        {
            transform = Transform.CreateRotation(vectorRight,
                isSameViewDirection ? -angleZ : angleZ) * transform;
        }

        var angleX = FindAngle(CurrentView.RightDirection, XYZ.BasisX);
        if (angleX != 0)
        {
            transform = Transform.CreateRotation(XYZ.BasisZ,
                isSameViewDirection ? -angleX : angleX) * transform;
        }

        return transform;
    }

    private void TransformCurves()
    {
        var rotation = this.CreateRotation();
        var translation = this.CreateTranslation();

        for (var i = 0; i < Curves.Count; i++)
        {
            Curves[i] = Curves[i].CreateTransformed(translation);
            Curves[i] = Curves[i].CreateTransformed(rotation);
        }
    }

    private Outline GetOutLineRebar()
    {
        var rightDirection = CurrentView.RightDirection;

        var points = new List<XYZ>();
        points.AddRange(_curves.Select(c => c.GetEndPoint(0)).ToList());
        points.AddRange(_curves.Select(c => c.GetEndPoint(1)).ToList());

        var minDistance = double.MaxValue;
        var maxDistance = double.MinValue;
        var minPoint = XYZ.Zero;
        var maxPoint = XYZ.Zero;
        foreach (var point in points)
        {
            var dot = point.DotProduct(rightDirection);
            if (dot <= minDistance)
            {
                minPoint = point;
                minDistance = dot;
            }

            if (dot > maxDistance)
            {
                maxPoint = point;
                maxDistance = dot;
            }
        }

        var outLine = new Outline(minPoint, maxPoint);

        return outLine;
    }
}