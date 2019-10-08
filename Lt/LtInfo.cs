using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Lt
{
    public class LtInfo : GH_AssemblyInfo
    {
        public override string Name => "Lt";

        public override Bitmap Icon => null;

        public override string Description => "一些景观方面的工具";

        public override Guid Id => new Guid("5c9c13be-588f-408e-9dc3-1922fcba732a");

        public override string AuthorName => "兰亭 & 林师兄";

        public override string AuthorContact => "1142060440@qq.com & 329978214@qq.com";
    }
}
