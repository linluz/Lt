using System.Drawing;
using System.Windows.Forms;
using GH_IO.Serialization;
using Lt.GHComponent.Analysis;

namespace Lt.GHComponent.Osboletes.Osb2;

// ReSharper disable once UnusedMember.Global
public sealed class LTMD_Osb2 : AOComponent
{
    public LTMD_Osb2()
        : base("坡向分析(网格)", "LTMD",
            "分析",
            ComponentID.LTMD_Osb2, nameof(LTMD), LTResource.山体坡向分析)
    {
        Shade = new MBooleanMenuItem(this, true, "使用面着色(&F)", true, rw: false);
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


    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        => Menu_Boolean(menu, ref Shade, click: (_, _) => Message = Shade.Def ? "面着色" : "顶点着色");



    public override bool Write(GH_IWriter writer)
    {
        writer.SetBoolean("面色否", Shade.Def);
        return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
        Shade.Def = reader.GetBoolean("面色否");
        return base.Read(reader);
    }

    private MBooleanMenuItem Shade;
}