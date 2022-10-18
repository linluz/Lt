using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace Lt
{
    public class LtInfo : GH_AssemblyInfo
    {
        public override string Name => "Lt";

        public override Bitmap Icon => LTResource.ltlogo24;

        public override string Description => "一些景观方面的工具";

        public override Guid Id => new Guid("5c9c13be-588f-408e-9dc3-1922fcba732a");

        public override string AuthorName => "兰亭 & 林师兄";

        public override string AuthorContact => "1142060440@qq.com & 329978214@qq.com";
        public override string Version => "0.3";
    }
    public class AssemblyPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.ComponentServer.AddCategoryIcon("Lt", LTResource.ltlogo16);
            return GH_LoadingInstruction.Proceed;
        }
    }
}
