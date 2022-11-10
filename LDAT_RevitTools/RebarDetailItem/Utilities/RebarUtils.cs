﻿using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RebarDetailItem.Utilities;

public static class RebarUtils
{
    private static List<double> GetParameterValueFromSegments(RebarShape rebarShape)
    {
        var parameterValues = new List<double>();
        var rebarShapeDefinitionBySegments = (RebarShapeDefinitionBySegments)rebarShape.GetRebarShapeDefinition();

        var parameters = rebarShape.Parameters;

        for (int i = 0; i < rebarShapeDefinitionBySegments.NumberOfSegments; i++)
        {
            var rebarShapeSegment = rebarShapeDefinitionBySegments.GetSegment(i);

            var rebarShapeConstraints = rebarShapeSegment.GetConstraints();

            foreach (var rebarShapeConstraint in rebarShapeConstraints)
            {
                if (rebarShapeConstraint is RebarShapeConstraintSegmentLength rebarShapeConstraintSegmentLength)
                {
                    var paramId = rebarShapeConstraintSegmentLength.GetParamId();

                    foreach (Parameter parameter in parameters)
                    {
                        if (paramId == parameter.Id)
                        {
                            parameterValues.Add(parameter.AsDouble());
                            break;
                        }
                    }
                }
            }
        }

        return parameterValues;
    }

    private static (double?,double?) GetHookLengths(Rebar rebar)
    {
        var rebarBendData = rebar.GetBendData();

        double? hookLengthStart = null;
        if (rebarBendData.HookLength0> 0)
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

    public static List<double> GetParameterValues(Rebar rebar)
    {
        var parameterValues = new List<double>();

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

        return parameterValues;
    }
}