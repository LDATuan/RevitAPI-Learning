using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LDATRevitTool.Utilities;

public class RebarFilter : ISelectionFilter
{
    public bool AllowElement(Element elem)
    {
        return elem is Rebar;
    }

    public bool AllowReference(Reference reference, XYZ position)
    {
        throw new NotImplementedException();
    }
}
