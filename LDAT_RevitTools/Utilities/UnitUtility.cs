using System;

namespace LDATRevitTool.Utilities ;

public static class UnitUtility
{
  public static double Feet2Millimeter(this double feet )
  {
    return feet * 304.8;
  }
  
  public static double Millimeter2Feet(this double millimeter )
  {
    return millimeter / 304.8;
  }
  
  public static double RadianToDegree(this double radian )
  {
    return radian * 180.0 / Math.PI;
  }
}