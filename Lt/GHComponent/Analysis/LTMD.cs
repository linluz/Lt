using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Lt.Majas;
using Rhino.Geometry;
using System.Windows.Forms;
using Lt.Base.Component;
using Lt.Base.Extensions;

namespace Lt.GHComponent.Analysis
{
    /// <summary>
    /// 网格坡向分析
    /// Slope Direction Analysis
    /// </summary>
    public sealed class LTMD : ADCComponent
    {
        public LTMD()
            : base("坡向分析(网格)", "LTMD",
                "山坡地形朝向分析,X轴向为东,Y轴向为北" +
                "\r\n双击：切换着色模式," +
                "\r\n注意：顶点着色会焊接网格顶点，可能导致顶点减少，" +
                "\r\n面着色会把面的每个顶点都解离出来，可能会导致顶点增加",
                "分析",
                ID.LTMD, 1, LTResource.山体坡向分析)
        {
            Shade = new MBooleanMenuItem(this, true, "使用面着色(&F)", true,
                mf: m => m.Def ? "面着色" : "顶点着色");
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "要进行坡向分析的山地地形网格");
            pm.AddIP(ParT.Colour, "色彩", "C",
                "各向的色彩，请按上、北、东北、东、东南、南、西南、西、西北的顺序连入9个色彩", ParamTrait.List,
                new[]
                {
                    Color.FromArgb(219, 219, 219),
                    Color.FromArgb(232, 77, 77),
                    Color.FromArgb(230, 168, 55),
                    Color.FromArgb(227, 227, 59),
                    Color.FromArgb(49, 222, 49),
                    Color.FromArgb(39, 219, 189),
                    Color.FromArgb(51, 162, 222),
                    Color.FromArgb(48, 48, 217),
                    Color.FromArgb(217, 46, 217)
                });

            pm.AddOP(ParT.Mesh, "网格", "M", "已根据坡向着色的地形网格");
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region 初始化 获取输入
            Mesh m = new Mesh();
            List<Color> c = new List<Color>(9);
            if (!DA.GetData(0, ref m)
                || !DA.GetDataList(1, c))
                return;
            if (c.Count != 9)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "输入色彩数量不等于9！不足已补充为最后一项,多出已忽视。");
                for (int i = c.Count; i < 9; i++)
                    c.Add(c[i - 1]);
            }
            #endregion
            if (Shade.Def)
            {
                //数量不对时计算面法向
                if (m.FaceNormals.Count != m.Faces.Count)
                    m.FaceNormals.ComputeFaceNormals();
                //计算色彩
                var cl = m.FaceNormals.Select(t => c[DShade(t)]).ToArray();
                //获取顶点列表
                Point3f[] vl = m.Vertices.ToArray();
                var fl = m.Faces.ToArray();
                var nl = m.FaceNormals.ToArray();
                //清理顶点及顶点色彩列表
                m.VertexColors.Clear();
                m.Vertices.Clear();
                m.Faces.Clear();
                m.Normals.Clear();
                for (var i = 0; i < fl.Length; i++)
                {
                    var tri = fl[i].IsTriangle;
                    for (int j = 0; j < 3; j++)
                    {
                        //添加顶点,并修改对应面索引
                        fl[i][j] = m.Vertices.Add(vl[fl[i][j]]);
                        //添加色彩
                        m.VertexColors.Add(cl[i]);
                        //添加法向
                        m.Normals.Add(nl[i]);
                    }

                    if (tri)
                        fl[i][3] = fl[i][2];
                    else
                    {
                        fl[i][3] = m.Vertices.Add(vl[fl[i][3]]);
                        m.VertexColors.Add(cl[i]);
                        m.Normals.Add(nl[i]);
                    }
                }
                m.Faces.AddFaces(fl);
            }
            else
            {
                m.VertexColors.Clear();//清理旧色彩
                m.Weld(DocumentAngleTolerance());//焊接顶点
                m.Normals.ComputeNormals();//计算顶点法向
                //根据顶点法向计算色彩并添加
                foreach (Vector3f t in m.Normals)
                    m.VertexColors.Add(c[DShade(t)]);
            }

            DA.SetData(0, m);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
            => Menu_Boolean(menu, ref Shade);

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || Hidden || !args.Display.SupportsShading)
                return; ///跳过非着色模式和，或参数不支持预览
            args.Draw1Meshes(0, this);
        }

        public int DShade(Vector3f v)
        {
            if (v.X == 0 && v.Y == 0) //上方
                return 0;
            double t = Math.Abs(v.Y / v.X);
            bool x = v.X > 0;
            bool y = v.Y > 0;
            if (t < Num1) //东西
                return x ? 3 : 7;
            if (t > Num2) //北南
                return y ? 1 : 5;
            if (x) //东北、东南
                return y ? 2 : 4;
            return y ? 8 : 6;
            //西北、西南
        }

        private static readonly double Num1 = Math.Tan(Math.PI / 8);
        private static readonly double Num2 = Math.Tan(Math.PI * 3 / 8);

        private MBooleanMenuItem Shade;
        public override MBooleanMenuItem DoubleClick => Shade;
    }
}
