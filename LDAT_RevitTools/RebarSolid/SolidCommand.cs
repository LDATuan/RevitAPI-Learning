using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RebarSolid.ViewModel;

[Transaction(TransactionMode.Manual)]
public class SolidCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uidoc = commandData.Application.ActiveUIDocument;
        var vm = new SolidViewModel(uidoc);
        vm.SolidView.ShowDialog();
        return Result.Succeeded;
    }
}

