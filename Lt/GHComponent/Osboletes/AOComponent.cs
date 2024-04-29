using System.Drawing;

namespace Lt.GHComponent.Osboletes;

/// <summary>
/// 过时组件：基类
/// </summary>
// ReSharper disable once UnusedMember.Global
public abstract class AOComponent(
    string name,
    string nickname,
    string subCategory,
    string id,
    string nname,
    Bitmap icon = null)
    : MOComponent(name, nickname, "", "LT", subCategory, id, nname, icon);