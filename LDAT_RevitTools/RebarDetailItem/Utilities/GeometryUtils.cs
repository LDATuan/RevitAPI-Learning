using Autodesk.Revit.ApplicationServices ;
using Autodesk.Revit.DB ;
using Autodesk.Revit.DB.Structure ;
using System ;
using System.Collections.Generic ;
using System.IO ;
using System.Linq ;
using System.Text ;
using System.Threading.Tasks ;
using System.Windows ;
using Utilities ;

namespace RebarDetailItem.Utilities ;

public static class GeometryUtils
{
  private static Transform CreateRotation( this Rebar rebar )
  {
    var activeView = rebar.Document.ActiveView ;
    if ( rebar.IsRebarInSection( activeView ) ) {
      System.Windows.MessageBox.Show( "rebar in section" ) ;
    }

    var transform = Transform.Identity ;

    var normal = rebar.GetShapeDrivenAccessor().Normal ;
    var dotZ = normal.DotProduct( XYZ.BasisZ ) ;


    //origin ???
    //view is perpencular with BasixZ
    var vectorRight = activeView.RightDirection ;
    var vectorUp = activeView.UpDirection ;

    var angle = vectorRight.AngleTo( XYZ.BasisX ) ;
    if ( dotZ.IsEqual( 0 ) ) {
      transform = Transform.CreateRotation( vectorRight, -Math.PI / 2 ) * transform ;
      var dotx = vectorRight.DotProduct( XYZ.BasisX ) ;
      var doty = vectorRight.DotProduct( XYZ.BasisY ) ;

      double newAngle = angle ;
      if ( dotx > 0 && doty > 0 ) {
        newAngle = -angle ;
      }
      else if ( dotx < 0 && doty > 0 ) {
        newAngle = angle + Math.PI / 2 ;
      }
      else if ( dotx > 0 && doty < 0  ) {
        newAngle = angle ;
      }
      else {
        newAngle = -( angle + Math.PI / 2 );
      }
      transform = Transform.CreateRotation( XYZ.BasisZ, newAngle ) * transform ;
    }
    //
    // var bb = rebar.get_BoundingBox( null ) ;
    // transform.Origin = bb.Min ;

    return transform ;
  }

  private static Transform CreateTranslate( this Rebar rebar )
  {
    var bb = rebar.get_BoundingBox( null ) ;

    var transform = Transform.Identity ;
    transform = Transform.CreateTranslation( XYZ.Zero - bb.Min ) * transform ;

    return transform ;
  }

  public static List<Curve> GetCurves( this Rebar rebar )
  {
    var curves = rebar.GetCenterlineCurves( false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0 ) ;

    var translate = rebar.CreateTranslate() ;
    for ( int i = 0 ; i < curves.Count ; i++ ) {
      curves[ i ] = curves[ i ].CreateTransformed( translate ) ;
    }

    var transform = rebar.CreateRotation() ;


    for ( int i = 0 ; i < curves.Count ; i++ ) {
      curves[ i ] = curves[ i ].CreateTransformed( transform ) ;
    }


    return curves.ToList() ;
  }


  public static void CreateDetailItem( this Autodesk.Revit.ApplicationServices.Application application,
    List<Curve> curves, int idRebar, int IdView )
  {
    var path = "C:\\ProgramData\\Autodesk\\RVT 2020\\Family Templates\\English\\Metric Detail Item.rft" ;

    var familyDocument = application.NewFamilyDocument( path ) ;

    Autodesk.Revit.Creation.FamilyItemFactory factory = familyDocument.FamilyCreate ;
    View famView = new FilteredElementCollector( familyDocument ).OfClass( typeof( View ) ).Cast<View>()
      .First( x => x.Name == "Ref. Level" ) ;

    using ( Transaction transaction = new Transaction( familyDocument ) ) {
      transaction.Start( "Create Detail Item" ) ;

      foreach ( var curve in curves ) {
        factory.NewDetailCurve( famView, curve ) ;
      }

      transaction.Commit() ;
    }


    var option = new SaveAsOptions() { OverwriteExistingFile = true, MaximumBackups = 1 } ;
    var fileName = "RebarId" + idRebar + "_ViewId_" + IdView + ".rfa" ;
    familyDocument.SaveAs( Path.Combine( @"D:\Workspace\Temp\Revit", fileName ), option ) ;
    familyDocument.Close( false ) ;
  }
}

class LoadFamilyOption : IFamilyLoadOptions
{
  public bool OnFamilyFound( bool familyInUse, out bool overwriteParameterValues )
  {
    overwriteParameterValues = true ;
    return true ;
  }

  public bool OnSharedFamilyFound( Family sharedFamily, bool familyInUse, out FamilySource source,
    out bool overwriteParameterValues )
  {
    source = FamilySource.Family ;
    overwriteParameterValues = true ;
    return true ;
  }
}