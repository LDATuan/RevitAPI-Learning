using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using LDATRevitTool.Utilities;

namespace LDATRevitTool.RebarDetailItem.Utilities;

public static class RebarUtils
{
    private static IEnumerable<double> GetParameterValueFromSegments(RebarShape rebarShape)
    {
        var parameterValues = new List<double>();
        var rebarShapeDefinitionBySegments = (RebarShapeDefinitionBySegments)rebarShape.GetRebarShapeDefinition();

        var parameters = rebarShape.Parameters;

        for (var i = 0; i < rebarShapeDefinitionBySegments.NumberOfSegments; i++)
        {
            var rebarShapeSegment = rebarShapeDefinitionBySegments.GetSegment(i);

            var rebarShapeConstraints = rebarShapeSegment.GetConstraints();

            foreach (var rebarShapeConstraint in rebarShapeConstraints) {
                if ( rebarShapeConstraint is not RebarShapeConstraintSegmentLength rebarShapeConstraintSegmentLength )
                    continue ;
                var paramId = rebarShapeConstraintSegmentLength.GetParamId();

                foreach (Parameter parameter in parameters) {
                    if ( paramId != parameter.Id ) continue ;
                    parameterValues.Add(parameter.AsDouble());
                    break;
                }
            }
        }

        return parameterValues;
    }

    private static (double?, double?) GetHookLengths(Rebar rebar)
    {
        var rebarBendData = rebar.GetBendData();

        double? hookLengthStart = null;
        if (rebarBendData.HookLength0 > 0)
        {
            hookLengthStart = rebar.get_Parameter(BuiltInParameter.REBAR_SHAPE_START_HOOK_LENGTH).AsDouble();
        }

        double? hookLengthEnd = null;
        if (rebarBendData.HookLength0 > 0)
        {
            hookLengthEnd = rebar.get_Parameter(BuiltInParameter.REBAR_SHAPE_END_HOOK_LENGTH).AsDouble();
        }

        return (hookLengthStart, hookLengthEnd);
    }

    private static void RoundingNumber(this RebarRoundingManager rebarRoundingManager, List<double> parameterValues)
    {
        var roundingNumber = rebarRoundingManager.ApplicableSegmentLengthRounding;
        if (roundingNumber.IsEqual(0)) roundingNumber = 10;

        for ( var i = 0 ; i < parameterValues.Count ; i++ ) {
            var para = parameterValues[ i ] ;
            var milliValue = para.Feet2Millimeter() ;
            var roundedValue = rebarRoundingManager.ApplicableSegmentLengthRoundingMethod switch
            {
                RoundingMethod.Nearest => Math.Round( milliValue / roundingNumber ) * roundingNumber,
                RoundingMethod.Up => Math.Ceiling( milliValue / roundingNumber ) * roundingNumber,
                RoundingMethod.Down => Math.Floor( milliValue / roundingNumber ) * roundingNumber,
                _ => Math.Round( milliValue / roundingNumber ) * roundingNumber
            } ;
            parameterValues[ i ] = roundedValue.Millimeter2Feet() ;
        }
    }

    public static List<double> GetParameterValues(this Rebar rebar)
    {
        var parameterValues = new List<double>();

        var rebarRoundingManager = rebar.GetReinforcementRoundingManager() ;
        var document = rebar.Document;

        var rebarShape = (RebarShape)document.GetElement(rebar.GetShapeId());
        var parameterValueFromSegments = GetParameterValueFromSegments(rebarShape);
        var (hookLengthStart, hookLengthEnd) = GetHookLengths(rebar);

        if (hookLengthStart is not null)
        {
            parameterValues.Add(hookLengthStart.Value);
        }

        parameterValues.AddRange(parameterValueFromSegments);

        if (hookLengthEnd is not null)
        {
            parameterValues.Add(hookLengthEnd.Value);
        }
        rebarRoundingManager.RoundingNumber(parameterValues);
        return parameterValues ;
    }
}
