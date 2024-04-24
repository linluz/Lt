using Rhino.Geometry;

namespace Lt.Base.Extensions
{
    public static class RhGeoExtensions
    {
        public static BoundingBox UnionPoint(this BoundingBox box, Point3d point)
        {
            box.Union(point);
            return box;
        }
    }
}
