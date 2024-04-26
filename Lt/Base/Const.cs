using System;
using System.Drawing;
using Grasshopper.GUI.Gradient;
using Rhino;
using Rhino.Geometry;

namespace Lt.Base
{
    internal class Const
    {
        /// <summary>
        /// 默认渐变
        /// </summary>
        internal static GH_Gradient Gradient0 = new GH_Gradient(
            new[] { 0, 0.2, 0.4, 0.6, 0.8, 1 },
            new[]
            {
                Color.FromArgb(45, 51, 87),
                Color.FromArgb(75, 107, 169),
                Color.FromArgb(173, 203, 249),
                Color.FromArgb(254, 244, 84),
                Color.FromArgb(234, 126, 0),
                Color.FromArgb(237, 53, 17)
            });

        /// <summary>
        /// road图层的索引，-1为不存在
        /// </summary>
        internal static int RoadLayerIndex => RhinoDoc.ActiveDoc.Layers.Find("road", true);

        /// <summary>
        /// 0-90区间，可选弧度或角度
        /// </summary>
        internal static Interval A0(bool usedegrees = true)
            => new Interval(0, usedegrees ? 90 : Math.PI / 2);

        internal static readonly bool Paral = Environment.ProcessorCount > 1;
    }
}
