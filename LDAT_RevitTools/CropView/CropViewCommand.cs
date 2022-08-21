using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;

[Transaction(TransactionMode.Manual)]
public class CropViewCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uiDocument = commandData.Application.ActiveUIDocument;
        var document = uiDocument.Document;

        //Pick more time
        while (true)
        {
            PickedBox pickedBox = null;
            try
            {
                pickedBox = uiDocument.Selection.PickBox(PickBoxStyle.Directional, "Draw Rectangular, Press ESC to exit");
            }
            catch
            {
                break;
            }

            if (pickedBox != null)
            {
                using (Transaction transaction = new Transaction(document))
                {
                    transaction.Start("Crop view by selection");

                    var view = document.ActiveView;

                    // Get Inverse transform of active view
                    var transform = view.CropBox.Transform.Inverse;

                    var pickedBoxMin = transform.OfPoint(pickedBox.Min);
                    var pickedBoxMax = transform.OfPoint(pickedBox.Max);

                    var minX = Math.Min(pickedBoxMin.X, pickedBoxMax.X);
                    var minY = Math.Min(pickedBoxMin.Y, pickedBoxMax.Y);

                    var maxX = Math.Max(pickedBoxMin.X, pickedBoxMax.X);
                    var maxY = Math.Max(pickedBoxMin.Y, pickedBoxMax.Y);

                    var boundingBoxMin = new XYZ(minX, minY, 0);
                    var boundingBoxMax = new XYZ(maxX, maxY, 0);

                    var boundingBox = new BoundingBoxXYZ();
                    boundingBox.Min = boundingBoxMin;
                    boundingBox.Max = boundingBoxMax;

                    view.CropBox = boundingBox;
                    view.CropBoxActive = true;
                    view.CropBoxVisible = true;
                    document.Regenerate();

                    transaction.Commit();
                }
            }
        }
        return Result.Succeeded;
    }
}

