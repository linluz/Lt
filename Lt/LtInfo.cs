using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Lt;

// ReSharper disable once UnusedMember.Global
public class LtInfo : GH_AssemblyInfo
{
    public override string Name => "Lt";

    public override Bitmap Icon => LTResource.ltlogo24;

    public override string Description => "一些景观方面的工具";

    public override Guid Id => new("5c9c13be-588f-408e-9dc3-1922fcba732a");

    public override string AuthorName => "兰亭 & 林师兄";

    public override string AuthorContact => "1142060440@qq.com & 329978214@qq.com";
    public override string Version => "0.5";
}