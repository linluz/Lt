using System.Drawing;
using Lt.Majas;

namespace Lt.Base.Component
{
    /// <summary>
    /// 电池基类，组为LT
    /// </summary>
    public abstract class ADCComponent : MDCComponent
    {
        protected ADCComponent(string name, string nickname, string description, string subCategory, string id, int exposure = 1, Bitmap icon = null) :
            base(name, nickname, description, "Lt", subCategory, id, exposure, icon)
        { }
    }
}
