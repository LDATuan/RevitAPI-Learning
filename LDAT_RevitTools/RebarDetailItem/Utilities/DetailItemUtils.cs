using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;
using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Text ;
using System.Threading.Tasks ;
using LDATRevitTool.RebarDetailItem.Models ;
using LDATRevitTool.Utilities ;
using Utilities ;

namespace LDATRevitTool.RebarDetailItem.Utilities ;

public static class DetailItemUtils
{
	public static XYZ PointInsert( this View view, XYZ origin, XYZ pick )
	{
		XYZ point ;
		if ( view.RightDirection.IsParallelTo( XYZ.BasisZ ) ) {
			point = new XYZ( origin.X, origin.Y, pick.Z ) ;
		}
		else {
			point = new XYZ( origin.X, origin.Y, pick.Z ) ;
		}

		return pick ;
	}

	public static Family CreateDetailItem( this Application application, Document targetDocument, RebarInfo rebarInfo )
	{
		const string path =
			"D:\\Github\\RevitAPI-Learning\\LDAT_RevitTools\\RebarDetailItem\\Data\\Revit 2020\\Metric Detail Item.rfa" ;

		var familyDocument = application.OpenDocumentFile( path ) ;

		var factory = familyDocument.FamilyCreate ;
		var refLevelView = new FilteredElementCollector( familyDocument ).OfClass( typeof( View ) ).Cast<View>()
			.First( x => x.Name == "Ref. Level" ) ;

		var textNoteInfo = new TextNoteInfo( familyDocument, refLevelView ) ;

		using ( var transaction = new Transaction( familyDocument ) ) {
			transaction.Start( "Create Detail Item" ) ;

			var index = 0 ;
			foreach ( var curve in rebarInfo.Curves ) {
				if ( curve is Line line ) {
					textNoteInfo.Insert( line, rebarInfo.ParameterValues[ index ] ) ;
					index++ ;
				}

				factory.NewDetailCurve( refLevelView, curve ) ;
			}

			transaction.Commit() ;
		}

		var option = new SaveAsOptions() { OverwriteExistingFile = true, MaximumBackups = 1 } ;
		var fileName = "RebarId_" + rebarInfo.Rebar.Id + "_ViewId_" + rebarInfo.CurrentView.Id + ".rfa" ;
		familyDocument.SaveAs( Path.Combine( @"D:\Workspace\Temp\Revit", fileName ), option ) ;

		var family = familyDocument.LoadFamily( targetDocument, new LoadFamilyOption() ) ;

		familyDocument.Close( false ) ;

		return family ;
	}

	public static void Insert( this Document document, Family family, XYZ point )
	{
		if ( document.GetElement( family.GetFamilySymbolIds().FirstOrDefault() ) is not FamilySymbol familySymbol ) {
			return ;
		}

		using Transaction transaction = new(document) ;
		transaction.Start( "Insert Detail Item" ) ;
		if ( ! familySymbol.IsActive ) {
			familySymbol.Activate() ;
		}

		document.Create.NewFamilyInstance( point, familySymbol, document.ActiveView ) ;
		transaction.Commit() ;
	}
}