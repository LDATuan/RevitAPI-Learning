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
        var document = uiDocument.Document;

        var refElement = uiDocument.Selection.PickObject(ObjectType.Element, new RebarFilter());

        var ele = document.GetElement(refElement) as Rebar;

        var rebarShapeInfo = new RebarShapeInfo(ele);
        var detailItemInfo = new DetailItemInfo(commandData.Application, rebarShapeInfo);
        var isSuccess = detailItemInfo.Insert();

        return isSuccess ? Result.Succeeded : Result.Cancelled;
    }
}