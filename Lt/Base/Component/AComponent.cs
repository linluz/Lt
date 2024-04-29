using System.Drawing;

namespace Lt.Base.Component;

/// <summary>
/// 主类别为Lt的MComponent基类
/// </summary>
/// <param name="name"></param>
/// <param name="nickname"></param>
/// <param name="description"></param>
/// <param name="subCategory"></param>
/// <param name="id"></param>
/// <param name="exposure"></param>
/// <param name="icon">图标</param>
public abstract class AComponent(
    string name,
    string nickname,
    string description,
    string subCategory,
    string id,
    int exposure = 1,
    Bitmap icon = null)
    : MComponent
    (
        name,
        nickname,
        description,
        "Lt",
        subCategory,
        id,
        exposure,
        icon
    );