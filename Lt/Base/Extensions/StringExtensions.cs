#nullable enable
using System.IO;
using System.Text;

namespace Lt.Base.Extensions;

/// <summary>
/// 扩展方法
/// </summary>
public static class StringExtension
{
    public static bool IsNullOrEmpty(this string? s)
        => string.IsNullOrEmpty(s);
    public static bool NotNullOrEmpty(this string? s)
        => !string.IsNullOrEmpty(s);
    public static bool IsNullOrWhiteSpace(this string? s)
        => string.IsNullOrWhiteSpace(s);
    public static bool NotNullOrWhiteSpace(this string? s)
        => !string.IsNullOrWhiteSpace(s);
    public static bool FileExists(this string s)
        => File.Exists(s);
    public static bool FileNotExists(this string s)
        => !File.Exists(s);
    public static bool DirExists(this string s)
        => Directory.Exists(s);
    public static bool DirNotExists(this string s)
        => !Directory.Exists(s);
    /// <summary>
    /// 字符转码
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static byte[] ToUtf8Byte(this string s)
        => Encoding.UTF8.GetBytes(s);

    /// <summary>
    /// 码转字符
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    public static string ToUtf8String(this byte[] b)
        => Encoding.UTF8.GetString(b);

    /// <summary>
    /// 给输入的本机路径或者远程路径增加长路径前缀
    /// </summary>
    /// <param name="path"></param>
    /// <param name="limit">是否判断长度，True则判断长度，超过250才加前缀,否则一定加前缀，默认为True</param>
    /// <returns></returns>
    public static string ToLongPath(this string path, bool limit = true)
    {
        if (path.IsNullOrEmpty()) return path;
        if (path.StartsWith(@"\\?\")) return path;
        return limit && path.Length < 255
            ? path
            : path.StartsWith(@"\\")
                ? @"\\?\UNC\" + path.Substring(2)
                : @"\\?\" + path;
    }
}