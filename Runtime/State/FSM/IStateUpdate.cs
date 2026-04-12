namespace CFramework
{
    public interface IStateUpdate
    {
        /// <summary>
        ///     状态更新
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        void OnUpdate(float deltaTime);
    }
}