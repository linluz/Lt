using System;
using System.Linq;
using Grasshopper.Kernel;
using Lt.Majas;
using Rhino.Geometry;
using System.Windows.Forms;
using Lt.Base;
using Lt.Base.Component;
using Lt.Base.Extensions;

namespace Lt.GHComponent.Analysis
{
    /// <summary>
    /// 网格坡度分析
    /// Terrain Mesh Grade Analysis
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public sealed class LTMG : GradientComponent, IMCom_DoubleClick
    {
        public LTMG() : base("坡度分析(网格)", "LTMG",
            "山地地形坡度分析,\r\n双击：【坡度范围】切换为角度/弧度",
            "分析",
            ID.LTMG, 1, LTResource.山体坡度分析)
        {
            Gra.Def = Const.Gradient0.Duplicate();
            Gra.ReCom = true;
            GI = new MBooleanMenuItem(this, true, "自适应范围(&A)", true, mf: m => m.Def ? "自适应" : "0-90°");
            UD = new MBooleanMenuItem(this, true, "使用角度(&U)", true, mf: m => m.Def ? "角度" : "弧度");
        }
        protected override void AddParameter(ParamManager pm)
        {
            pm.AddIP(ParT.Mesh, "地形", "M", "要进行坡度分析的山地地形网格");

            pm.AddOP(ParT.Mesh, "地形", "M", "已按角度着色的地形网格");
            pm.AddOP(ParT.Interval, "角度", "A", "坡度范围（度）");
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.OutDataC(0, out Mesh tm))
                return;
            if (tm.Normals.Count != tm.Vertices.Count)
                tm.Normals.ComputeNormals();//计算法向
            tm.VertexColors.Clear();//清除色彩

            double d = UD.Def ? Majas_Ex.R2A : 1;//输出弧度还是角度
            var ra = tm.Normals.Select(t => Math.Round(Math.Acos(t.Z) * d, 2)).ToArray();//法向转角度,保留两位小数


            var ia = ra.ToInterval();//获取角度范围
            Interval ib = GI.Def ? ia : Const.A0(UD.Def);//着色范围
            //角度转换为色彩并给予网格
            tm.VertexColors.AppendColors(ra.Select(t => Dou2Col(ib, t)).ToArray());
            DA.SetData(0, tm);
            DA.SetData(1, ia);
        }
        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_Boolean(menu, ref GI, "默认启用，此时渐变色彩范围对应实际的角度范围。\r\n不启用时，范围对应0-90º");
            Menu_Boolean(menu, ref UD, "勾选时,【角度】输出端输出度,否则为弧度", LTResource.Degrees_16,
                (s, e) => UD.ReplacePDesc(true, "（弧度）", "（度）", 1));
        }

        public override void CreateAttributes()
            => m_attributes = new DoubleClick_Attributes(this);
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (args.Document.PreviewMode != GH_PreviewMode.Shaded || Hidden || !args.Display.SupportsShading)
                return; ///跳过非着色模式和，或参数不支持预览
            args.Draw1Meshes(0, this);
        }

        private MBooleanMenuItem GI;
        private MBooleanMenuItem UD;
        public MBooleanMenuItem DoubleClick => UD;
    }
}
