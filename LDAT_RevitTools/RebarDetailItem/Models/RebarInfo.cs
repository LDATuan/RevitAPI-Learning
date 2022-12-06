using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LDATRevitTool.Utilities;

namespace LDATRevitTool.RebarDetailItem.Models;

public class RebarInfo
{
    private readonly View _currentView;

    public Rebar Rebar { get; }

    public XYZ Normal { get; }

    public XYZ RightDirection { get; set; }

    public List<Curve> Curves { get; private set; }

    public RebarInfo(Rebar rebar)
    {
        _currentView = rebar.Document.ActiveView;
        this.Rebar = rebar;
        this.Normal = rebar.GetShapeDrivenAccessor().Normal;
        
        this.Curves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0).ToList();
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
            this.RightDirection = XYZ.BasisZ.CrossProduct(this.Normal); ;
        }
    }

    private Transform CreateTranslation()
    {
        var transform = Transform.Identity;
        var lowerLeftPoint = this.GetLowerLeftPoint();

        transform = Transform.CreateTranslation(XYZ.Zero - lowerLeftPoint) * transform;

        return transform;
    }

    private double FindAngle(XYZ vector1, XYZ vector2)
    {
        var dotAngle = vector1.DotProduct(vector2);
        var angle = vector1.AngleTo(vector2);
        if (angle != 0)
        {
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

        var angleZ = FindAngle(Normal, XYZ.BasisZ);
        if (angleZ != 0)
        {
            transform = Transform.CreateRotation(vectorRight, angleZ) * transform;
        }

        var angleX = FindAngle(_currentView.RightDirection, XYZ.BasisX);
        if (angleX != 0)
        {
            transform = Transform.CreateRotation(XYZ.BasisZ, angleX) * transform;
        }

        return transform;
    }

    private void TransformCurves()
    {
        var rotation = this.CreateRotation();
        var translation = this.CreateTranslation();

        for (int i = 0; i < Curves.Count; i++)
        {
            Curves[i] = Curves[i].CreateTransformed(translation);
            Curves[i] = Curves[i].CreateTransformed(rotation);
        }
    }

    public XYZ GetLowerLeftPoint()
    {
        var bb = _currentView.get_BoundingBox(null);
        var bbMin = bb.Min;

        var points = new List<XYZ>();

        points.AddRange(this.Curves.Select(c => c.GetEndPoint(0)).ToList());
        points.AddRange(this.Curves.Select(c => c.GetEndPoint(1)).ToList());

        double minDistance = 1000000;
        var originPoint = XYZ.Zero;
        foreach (var point in points)
        {
            var distance = point.DistanceTo(bbMin);
            if (distance > minDistance) continue;
            originPoint = point;
            minDistance = distance;
        }

        return originPoint;
    }
}