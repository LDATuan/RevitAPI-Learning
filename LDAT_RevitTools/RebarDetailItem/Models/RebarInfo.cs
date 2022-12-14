using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LDATRevitTool.RebarDetailItem.Utilities;
using LDATRevitTool.Utilities;

namespace LDATRevitTool.RebarDetailItem.Models;

public class RebarInfo
{
    private readonly IEnumerable<Curve> _curves;
    public View CurrentView { get; }

    public Rebar Rebar { get; }

    public XYZ Normal { get; }

    public XYZ RightDirection { get; set; }

    public List<Curve> Curves { get; private set; }

    public List<double> ParameterValues { get; }

    public RebarInfo(Rebar rebar)
    {
        CurrentView = rebar.Document.ActiveView;
        this.Rebar = rebar;
        this.Normal = rebar.GetShapeDrivenAccessor().Normal;
        this.ParameterValues = rebar.GetParameterValues();

        _curves = rebar.GetCenterlineCurves(false , false , false , MultiplanarOption.IncludeOnlyPlanarCurves , 0);
        this.Curves = _curves.ToList();
        this.FindRightDirection();
        this.TransformCurves();
    }

    private void FindRightDirection()
    {
        if (this.Normal.IsParallelTo(XYZ.BasisZ)) {
            this.RightDirection = this.Normal.IsSameDirection(XYZ.BasisZ) ? XYZ.BasisX : -XYZ.BasisX;
        }
        else {
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

    private double FindAngle(XYZ vector1 , XYZ vector2)
    {
        var dotAngle = vector1.DotProduct(vector2);
        var angle = vector1.AngleTo(vector2);
        if (angle != 0) {
            //var newAngle = angle.IsLessThan(Math.PI / 2) ? -angle : -(Math.PI - angle);
            var newAngle = -angle;

            //var newAngle = dotAngle switch
            //{
            //    >= 0 when angle.IsLessThan(Math.PI / 2) => -angle,
            //    >= 0 when angle.IsGreater(Math.PI / 2) => -(Math.PI - angle),
            //    < 0 when angle.IsLessThan(Math.PI / 2) => -angle,
            //    _ => Math.PI - angle
            //};
            return newAngle;
        }

        return 0;
    }

    private Transform CreateRotation()
    {
        var transform = Transform.Identity;
        var vectorRight = XYZ.BasisZ.CrossProduct(this.Normal);

        var angleZ = FindAngle(Normal , XYZ.BasisZ);
        if (angleZ != 0) {
            transform = Transform.CreateRotation(vectorRight , angleZ) * transform;
        }

        var angleX = FindAngle(CurrentView.RightDirection , XYZ.BasisX);
        if (angleX != 0) {
            transform = Transform.CreateRotation(XYZ.BasisZ , angleX) * transform;
        }

        return transform;
    }

    private void TransformCurves()
    {
        var rotation = this.CreateRotation();
        var translation = this.CreateTranslation();

        for (var i = 0 ; i < Curves.Count ; i++) {
            Curves[ i ] = Curves[ i ].CreateTransformed(translation);
            Curves[ i ] = Curves[ i ].CreateTransformed(rotation);
        }
    }

    public Outline GetOutLineRebar()
    {
        var rightDirection = CurrentView.RightDirection;

        var points = new List<XYZ>();
        points.AddRange(_curves.Select(c => c.GetEndPoint(0)).ToList());
        points.AddRange(_curves.Select(c => c.GetEndPoint(1)).ToList());

        var minDistance = double.MaxValue;
        var maxDistance = double.MinValue;
        var minPoint = XYZ.Zero;
        var maxPoint = XYZ.Zero;
        foreach (var point in points) {
            var dot = point.DotProduct(rightDirection);
            if (dot <= minDistance) {
                minPoint = point;
                minDistance = dot;
            }

            if (dot > maxDistance) {
                maxPoint = point;
                maxDistance = dot;
            }
        }

        var outLine = new Outline(minPoint , maxPoint);

        return outLine;
    }
}