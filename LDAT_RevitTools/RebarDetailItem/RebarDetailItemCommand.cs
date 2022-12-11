﻿using Autodesk.Revit.Attributes ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using Autodesk.Revit.UI ;
using Autodesk.Revit.UI.Selection ;
using System ;
using System.Collections.Generic ;
using System.Linq ;
using System.Text ;
using System.Threading.Tasks ;
using LDATRevitTool.RebarDetailItem.Utilities ;
using LDATRevitTool.Utilities ;
using LDATRevitTool.RebarDetailItem.Models ;

[Transaction( TransactionMode.Manual )]
public class RebarDetailItemCommand : IExternalCommand
{
	public Result Execute( ExternalCommandData commandData, ref string message, ElementSet elements )
	{
		var uiDocument = commandData.Application.ActiveUIDocument ;
		var application = commandData.Application.Application ;
		var document = uiDocument.Document ;

		var refElement = uiDocument.Selection.PickObject( ObjectType.Element, new RebarFilter() ) ;

		var ele = document.GetElement( refElement ) as Rebar ;

		var rebarInfo = new RebarInfo( ele ) ;
		var lowerLeftPoint = rebarInfo.GetLowerLeftPoint() ;

		var detailItemInfo = new DetailItemInfo( application, rebarInfo ) ;
		var family = detailItemInfo.CreateOrUpdate() ;
		
		if ( detailItemInfo.IsFamilyInProject ) return Result.Succeeded ;
		var pickPoint = uiDocument.Selection.PickPoint() ;

		var pointInsert = document.ActiveView.PointInsert( lowerLeftPoint, pickPoint ) ;

		document.Insert( family, pointInsert ) ;

		return Result.Succeeded ;
	}
}