namespace Lt.Base.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string str)
            => string.IsNullOrEmpty(str);
        public static bool NotNullOrEmpty(this string str)
            => !string.IsNullOrEmpty(str);
        public static bool IsNullOrWhiteSpace(this string str)
            => string.IsNullOrWhiteSpace(str);
        public static bool NotNullOrWhiteSpace(this string str)
            => !string.IsNullOrWhiteSpace(str);
    }
}
