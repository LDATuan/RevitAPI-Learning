using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using LDATRevitTool.Utilities;
using LDATRevitTool.RebarDetailItem.Models;

[Transaction(TransactionMode.Manual)]
public class RebarDetailItemCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDocument = commandData.Application.ActiveUIDocument;
        var application = commandData.Application.Application;
        var document = uiDocument.Document;

        var refElement = uiDocument.Selection.PickObject(ObjectType.Element, new RebarFilter());

        var ele = document.GetElement(refElement) as Rebar;

        var rebarInfo = new RebarInfo(ele);
        var detailItemInfo = new DetailItemInfo(commandData.Application, rebarInfo);
        detailItemInfo.Insert();

        return Result.Succeeded;
    }
}