using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
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

        var refele = uiDocument.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element);

        var ele = document.GetElement(refele) as Rebar;

        var curves = ele.GetCurves();

        application.CreateDetailItem( curves ,ele.Id.IntegerValue, document.ActiveView.Id.IntegerValue) ;


        return Result.Succeeded;
    }
}
