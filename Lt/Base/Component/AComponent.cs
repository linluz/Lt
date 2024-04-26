using System.Drawing;
using Lt.Majas;

namespace Lt.Base.Component
{
    public abstract class AComponent : MComponent
    {
        protected AComponent(string name, string nickname, string description, string subCategory, string id, int exposure = 1, Bitmap icon = null) :
            base(name, nickname, description, "Lt", subCategory, id, exposure, icon)
        { }
    }
}
