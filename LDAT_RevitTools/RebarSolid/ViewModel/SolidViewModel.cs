using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RebarSolid.View;
using Utilities;

namespace RebarSolid.ViewModel
{
    public class SolidViewModel : ViewModelBase
    {
        private Document Doc { get; }
        private UIDocument UIDoc { get; }
        private SolidView solidView;

        public SolidView SolidView
        {
            get
            {
                if (solidView == null)
                {
                    solidView = new SolidView() { DataContext = this };
                }
                return solidView;
            }
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
        private int selectedIndex;

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                selectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
            }
        }

        public RelayCommand<object> ButtonRun { get; set; }

        public SolidViewModel(UIDocument uidoc)
        {
            UIDoc = uidoc;
            Doc = uidoc.Document;
            ButtonRun = new RelayCommand<object>(p => true, p => ButtonRunAction());
        }

        private void ButtonRunAction()
        {

            this.SolidView.Close();
            try
            {
                // Get cuurent view
                var currentView = Doc.ActiveView;

                // This tool only apply for 3D View
                if (currentView is View3D view3D)
                {

                    // Get all rebar in current view
                    IEnumerable<Rebar> rebars = null;
                    if (SelectedIndex == 0)
                    {
                        rebars = new FilteredElementCollector(Doc, currentView.Id).OfClass(typeof(Rebar)).Cast<Rebar>();
                    }
                    else
                    {
                        try
                        {
                            rebars = UIDoc.Selection.PickObjects(ObjectType.Element, new RebarFilter(), "Select Rebar").Select(x => Doc.GetElement(x)).Cast<Rebar>();
                        }
                        catch
                        { }
                    }
                    if (rebars == null) return;
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
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
        }
    }
    public class RebarFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return (elem.Category != null && elem is Rebar);
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new NotImplementedException();
        }
    }
}
