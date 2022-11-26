﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RebarDetailItem.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Transaction(TransactionMode.Manual)]
public class RebarDetailItemCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {

        var uiDocument = commandData.Application.ActiveUIDocument;
        var application = commandData.Application.Application;
        var document = uiDocument.Document;

        var refele = uiDocument.Selection.PickObject(ObjectType.Element);

        var ele = document.GetElement(refele) as Rebar;


        var curves = ele.GetCurves(out XYZ origin);

        var family = application.CreateDetailItem(document, curves, ele.Id.IntegerValue, document.ActiveView.Id.IntegerValue);

        var pickPoint = uiDocument.Selection.PickPoint();

        var pointInsert = document.ActiveView.PointInsert(origin, pickPoint);

        document.Insert(family, pointInsert);

        return Result.Succeeded;
    }
}
