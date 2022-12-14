using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;
using LDATRevitTool.Utilities ;

namespace LDATRevitTool.RebarDetailItem.Models ;

public class DetailItemInfo
{
	private readonly Application _application ;
	private readonly RebarInfo _rebarInfo ;
	private readonly Document _document ;
	private readonly string _name ;

	private Family _familyInProject ;
	public bool IsFamilyInProject { get ; private set ; }

	public DetailItemInfo( Application application, RebarInfo rebarInfo )
	{
		_application = application ;
		_rebarInfo = rebarInfo ;
		_document = rebarInfo.Rebar.Document ;
		_name = "RebarId_" + rebarInfo.Rebar.Id + "_viewId_" + rebarInfo.CurrentView.Id ;
		IsFamilyInProject = CheckFamilyInProject() ;
	}

	private bool CheckFamilyInProject()
	{
		var element = new FilteredElementCollector( _document ).OfClass( typeof( Family ) )
			.FirstOrDefault( f => f.Name == _name ) ;
		if ( element is null ) {
			return false ;
		}
		else {
			_familyInProject = (Family) element ;
			return true ;
		}
	}

	private Document GetFamilyDocument()
	{
		Document familyDocument ;
		if ( this.IsFamilyInProject ) {
			familyDocument = _document.EditFamily( _familyInProject ) ;
			var categoryFilter = new ElementMulticategoryFilter(
				new List<BuiltInCategory>() { BuiltInCategory.OST_Lines, BuiltInCategory.OST_GenericAnnotation } ) ;
			var collector = new FilteredElementCollector( familyDocument ).WhereElementIsNotElementType()
				.WherePasses( categoryFilter ).ToElementIds() ;
			using var transaction = new Transaction( familyDocument ) ;
			transaction.Start( "Clean" ) ;
			familyDocument.Delete( collector ) ;
			transaction.Commit() ;
		}
		else {
			// const string path =
			// 	"D:\\Github\\RevitAPI-Learning\\LDAT_RevitTools\\RebarDetailItem\\Data\\Revit 2020\\Metric Detail Item.rfa" ;

			const string path =
				"D:\\3. TUAN LE\\01. Github\\RevitAPI-Learning\\LDAT_RevitTools\\RebarDetailItem\\Data\\Revit 2020\\Metric Detail Item.rfa";
			
			
			familyDocument = _application.OpenDocumentFile( path ) ;
		}

		return familyDocument ;
	}

	public Family CreateOrUpdate()
	{
		var familyDocument = this.GetFamilyDocument() ;
		var factory = familyDocument.FamilyCreate ;
		var refLevelView = new FilteredElementCollector( familyDocument ).OfClass( typeof( View ) ).Cast<View>()
			.First( x => x.Name == "Ref. Level" ) ;

		var textNoteInfo = new TextNoteInfo( familyDocument, refLevelView ) ;

		using ( var transaction = new Transaction( familyDocument ) ) {
			transaction.Start( "Create Detail Item" ) ;

			var index = 0 ;
			foreach ( var curve in _rebarInfo.Curves ) {
				if ( curve is Line line ) {
					textNoteInfo.Insert( line, _rebarInfo.ParameterValues[ index ] ) ;
					index++ ;
				}

				factory.NewDetailCurve( refLevelView, curve ) ;
			}

			transaction.Commit() ;
		}

		var option = new SaveAsOptions() { OverwriteExistingFile = true, MaximumBackups = 1 } ;
		var fileName = this._name + ".rfa" ;
		familyDocument.SaveAs( Path.Combine( @"D:\Workspace\Temp\Revit", fileName ), option ) ;

		var family = familyDocument.LoadFamily( _document, new LoadFamilyOption() ) ;

		familyDocument.Close( false ) ;

		return family ;
	}
}