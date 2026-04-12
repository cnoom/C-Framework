namespace CFramework.Runtime.UI
{
    public interface IUI
    {
        /// <summary>
        ///     注入UI组件
        /// </summary>
        /// <param name="binder"></param>
        void InjectUI(UIBinder binder);

        /// <summary>
        ///     UI创建完成后调用（生命周期方法）
        /// </summary>
        void OnCreate();

        /// <summary>
        ///     显示UI时调用
        /// </summary>
        void OnShow();

        /// <summary>
        ///     隐藏UI时调用
        /// </summary>
        void OnHide();

        /// <summary>
        ///     销毁UI时调用
        /// </summary>
        void OnDestroy();
    }
}