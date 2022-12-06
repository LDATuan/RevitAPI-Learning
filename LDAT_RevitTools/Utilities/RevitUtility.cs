using System;
using Autodesk.Revit.DB;

namespace LDATRevitTool.Utilities;

public static class RevitUtility
{
    private const double Precision = 0.001;

    public static bool IsEqual(this double a, double b)
    {
        return Math.Abs(a - b) < Precision;
    }

    public static bool IsGreaterThan(this double a, double b)
    {
        return a - b >= Precision;
    }

    public static bool IsGreater(this double a, double b)
    {
        return a - b > Precision;
    }

    public static bool IsLessThan(this double a, double b)
    {
        return a - b <= Precision;
    }
    public static bool IsLess(this double a, double b)
    {
        return a - b < Precision;
    }

    public static bool IsSameDirection(this XYZ source, XYZ target)
    {
        var dot = source.DotProduct(target);
        return dot.IsEqual(1);
    }
    
    public static bool IsParallelTo(this XYZ source, XYZ target)
    {
        var dot = source.DotProduct(target);
        return dot.IsEqual(1) || dot.IsEqual(-1);
    }
}

public class LoadFamilyOption : IFamilyLoadOptions
{
    public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
    {
        overwriteParameterValues = true;
        return true;
    }

    public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source,
      out bool overwriteParameterValues)
    {
        source = FamilySource.Family;
        overwriteParameterValues = true;
        return true;
    }
}
