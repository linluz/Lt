using System.Drawing;

namespace Lt.Base.Component;

/// <summary>
/// 电池基类，组为LT,自带双击相关功能
/// </summary>
/// <param name="name"></param>
/// <param name="nickname"></param>
/// <param name="description"></param>
/// <param name="subCategory"></param>
/// <param name="id"></param>
/// <param name="exposure"></param>
/// <param name="icon"></param>
public abstract class ADCComponent(
    string name,
    string nickname,
    string description,
    string subCategory,
    string id,
    int exposure = 1,
    Bitmap icon = null)
    : MDCComponent
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