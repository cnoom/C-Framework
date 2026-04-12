namespace CFramework
{
    /// <summary>
    ///     类型安全的黑板键
    /// </summary>
    /// <typeparam name="T">键关联的值类型</typeparam>
    public readonly struct BlackboardKey<T>
    {
        /// <summary>
        ///     键名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     创建黑板键
        /// </summary>
        /// <param name="name">键名称</param>
        public BlackboardKey(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"BlackboardKey<{typeof(T).Name}>({Name})";
        }

        public override int GetHashCode()
        {
            return (Name, typeof(T)).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is BlackboardKey<T> other && other.Name == Name && other.GetType() == GetType();
        }

        public static bool operator ==(BlackboardKey<T> left, BlackboardKey<T> right)
        {
            return left.Name == right.Name;
        }

        public static bool operator !=(BlackboardKey<T> left, BlackboardKey<T> right)
        {
            return left.Name != right.Name;
        }
    }
}