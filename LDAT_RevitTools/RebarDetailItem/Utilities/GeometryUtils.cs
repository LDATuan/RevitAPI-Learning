using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Utilities;

namespace RebarDetailItem.Utilities;

public static class GeometryUtils
{
    private static Transform CreateRotation(this Rebar rebar)
    {
        var activeView = rebar.Document.ActiveView;
        if (rebar.IsRebarInSection(activeView))
        {
            System.Windows.MessageBox.Show("rebar in section");
        }

        var transform = Transform.Identity;

        var normal = rebar.GetShapeDrivenAccessor().Normal;
        var dotZ = normal.DotProduct(XYZ.BasisZ);


        //origin ???
        //view is perpencular with BasixZ
        var vectorRight = activeView.RightDirection;
        var vectorUp = activeView.UpDirection;

        var angle = vectorRight.AngleTo(XYZ.BasisX);
        if (dotZ.IsEqual(0))
        {
            transform = Transform.CreateRotation(vectorRight, -Math.PI / 2) * transform;
            var dotx = vectorRight.DotProduct(XYZ.BasisX);
            var doty = vectorRight.DotProduct(XYZ.BasisY);

            double newAngle = angle;
            if (dotx > 0 && doty > 0)
            {
                newAngle = -angle;
            }
            else if (dotx < 0 && doty > 0)
            {
                newAngle = angle + Math.PI / 2;
            }
            else if (dotx > 0 && doty < 0)
            {
                newAngle = angle;
            }
            else
            {
                newAngle = -(angle + Math.PI / 2);
            }
            transform = Transform.CreateRotation(XYZ.BasisZ, newAngle) * transform;
        }
        //
        // var bb = rebar.get_BoundingBox( null ) ;
        // transform.Origin = bb.Min ;

        return transform;
    }

    public static XYZ GetLowerLeftPoint(this View view, IList<Curve> curves)
    {
        var bb = view.get_BoundingBox(null);
        var bbMin = bb.Min;

        var points = new List<XYZ>();

        points.AddRange(curves.Select(c => c.GetEndPoint(0)).ToList());
        points.AddRange(curves.Select(c => c.GetEndPoint(1)).ToList());


        double minDistance = 1000000;
        XYZ originpoint = XYZ.Zero;
        foreach (var point in points)
        {
            var distance = point.DistanceTo(bbMin);
            if (distance < minDistance)
            {
                originpoint = point;
                minDistance = distance;
            }
        }
        return originpoint;
    }

    private static Transform CreateTranslate(this XYZ originpoint)
    {
        var transform = Transform.Identity;
        transform = Transform.CreateTranslation(XYZ.Zero - originpoint) * transform;

        return transform;
    }


    public static List<Curve> GetCurves(this Rebar rebar, out XYZ lowerLeftPoint)
    {
        var curves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0);


        lowerLeftPoint = rebar.Document.ActiveView.GetLowerLeftPoint(curves);

        var rotateTransform = rebar.CreateRotation();
        var translate = CreateTranslate(lowerLeftPoint);

        for (int i = 0; i < curves.Count; i++)
        {
            curves[i] = curves[i].CreateTransformed(translate);
        }



        for (int i = 0; i < curves.Count; i++)
        {
            curves[i] = curves[i].CreateTransformed(rotateTransform);
        }


        return curves.ToList();
    }
}

