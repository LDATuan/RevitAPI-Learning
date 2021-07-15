using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using RebarSolid.View;
using Utilities;

namespace RebarSolid.ViewModel
{
   public class SolidViewModel : ViewModelBase
    {
        private Document Doc { get; }
        private SolidView solidView;

        public SolidView SolidView
        {
            get {
                if (solidView ==null)
                {
                    solidView = new SolidView() { DataContext = this };
                }
                return solidView; }
            set
            {
                solidView = value;
                OnPropertyChanged(nameof(SolidView));
            }
        }

        private bool isCheckedSolid;

        public bool IsCheckedSolid
        {
            get { return isCheckedSolid; }
            set
            {
                isCheckedSolid = value;
                OnPropertyChanged(nameof(IsCheckedSolid));
            }
        }
        private bool isCheckedUnobscured;

        public bool IsCheckedUnobscured
        {
            get { return isCheckedUnobscured; }
            set
            {
                isCheckedUnobscured = value;
                OnPropertyChanged(nameof(IsCheckedUnobscured));
            }
        }
        public RelayCommand<object> ButtonRun { get; set; }

        public SolidViewModel(Document doc)
        {
            Doc = doc;
            ButtonRun = new RelayCommand<object>(p => true, p => ButtonRunAction());
        }

        private void ButtonRunAction()
        {
            this.SolidView.Close();
            // Get cuurent view
            var currentView = Doc.ActiveView;

            // This tool only apply for 3D View

            if (currentView is View3D view3D)
            {
                // Get all rebar in current view

                var rebars = new FilteredElementCollector(Doc, currentView.Id).OfClass(typeof(Rebar)).Cast<Rebar>();
                // All change in Revit need a transaction
                using (Transaction tx = new Transaction(Doc))
                {
                    tx.Start("Rebar Solid");
                    foreach (var rebar in rebars)
                    {
                        rebar.SetSolidInView(view3D, IsCheckedSolid);
                        rebar.SetUnobscuredInView(view3D, IsCheckedUnobscured);
                    }
                    tx.Commit();
                }
            }
            else
            {
                TaskDialog.Show("Solid Rebar", "Please open View 3D");
            }
        }
    }
}
