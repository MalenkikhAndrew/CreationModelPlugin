using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            List<Level> listLevel = new FilteredElementCollector(doc)
                                        .OfClass(typeof(Level))
                                        .OfType<Level>()
                                        .ToList();
            Level level1 = listLevel
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();
            Level level2 = listLevel
                .Where(x => x.Name.Equals("Уровень 2"))
                .FirstOrDefault();




            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double height = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;
            double dz = height / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }


            AddDoor(doc, level1, walls[0]);
            AddWindow(doc, level1, walls[1], dz);
            AddWindow(doc, level1, walls[2], dz);
            AddWindow(doc, level1, walls[3], dz);


            transaction.Commit();

            //    ElementId id = doc.GetDefaultElementTypeId(ElementTypeGroup.RoofType);
            //    RoofType type = doc.GetElement(id) as RoofType;
            //    if (type == null)
            //    {
            //        TaskDialog.Show("Error", "Not RoofType");
            //        return Result.Failed;
            //    }


            //    // Crear esquema
            //    CurveArray curveArray = new CurveArray();
            //    curveArray.Append(Line.CreateBound(new XYZ(dx, -dy-1, dz*2-(2.2)), new XYZ(dx, 0, dz*2 +5)));
            //    curveArray.Append(Line.CreateBound(new XYZ(dx, 0, dz*2 +5), new XYZ(dx, dy+1, dz*2-(2.2))));
            //    // Obtener la elevación de la vista actual
            //    Level level = doc.ActiveView.GenLevel;
            //    if (level == null)
            //    {
            //        TaskDialog.Show("Error", "No es PlainView");
            //        return Result.Failed;
            //    }

            //    // Crear techo
            //    using (Transaction tr = new Transaction(doc))
            //    {
            //        tr.Start("Create ExtrusionRoof");

            //        ReferencePlane plane =
            //            doc.Create.NewReferencePlane(new XYZ(-dx, -dy, dz),
            //                                         new XYZ(-dx, -dy, (dz + dz / 2)),
            //                                         new XYZ(0, -1, 0),
            //                                         doc.ActiveView);
            //        doc.Create.NewExtrusionRoof(curveArray, plane, level2, type, -width-1, 1);
            //        tr.Commit();

            return Result.Succeeded;

        }


        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .OfType<FamilySymbol>()
                    .Where(x => x.Name.Equals("0915 x 2134 мм"))
                    .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                    .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;


            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);



        }
        private void AddWindow(Document doc, Level level1, Wall wall, double dz)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_Windows)
                    .OfType<FamilySymbol>()
                    .Where(x => x.Name.Equals("0610 x 1830 мм"))
                    .Where(x => x.FamilyName.Equals("Фиксированные"))
                    .FirstOrDefault();



            LocationCurve hostCurve = wall.Location as LocationCurve;

            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;
            point = new XYZ(point.X, point.Y, dz / 2);





            if (!windowType.IsActive)
                windowType.Activate();

            doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);



        }

    }
 
}
