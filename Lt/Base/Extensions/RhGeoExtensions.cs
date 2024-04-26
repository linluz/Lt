using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;

namespace Lt.Base.Extensions
{
    public static class RhGeoExtensions
    {
#if DEBUG
        /// <summary>
        /// y向移动曲线2000单位，测试使用，方便linq使用
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Curve Move(this Curve c)
        {
            c.Translate(0, 2000, 0);
            return c;
        }
#endif
        /// <summary>
        /// 使外包盒合并一个点
        /// </summary>
        /// <param name="box">外包盒</param>
        /// <param name="point">要合并的点</param>
        /// <returns></returns>
        public static BoundingBox UnionPoint(this BoundingBox box, Point3d point)
        {
            box.Union(point);
            return box;
        }
        /// <summary>
        /// 使外包盒合并点群
        /// </summary>
        /// <param name="box">外包盒</param>
        /// <param name="point">要合并的点</param>
        /// <returns></returns>
        public static BoundingBox UnionPoints(this BoundingBox box, IEnumerable<Point3d> point)
            => point.Aggregate(box, (s, i) => s.UnionPoint(i));

        /// <summary>
        /// 将向量转换成坡度
        /// </summary>
        /// <param name="v">向量</param>
        /// <returns>转换后的弧度</returns>
        public static double VectorToSlope(this Vector3d v) => Math.Asin(v.Z < 0 ? -v.Z : v.Z);

        public static Vector3d ToUnitize(this Vector3d v)
        {
            v.Unitize();
            return v;
        }

    }
}