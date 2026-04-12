namespace CFramework
{
    public interface IStateFixedUpdate
    {
        /// <summary>
        ///     固定更新
        /// </summary>
        /// <param name="fixedDeltaTime">固定帧间隔时间</param>
        void OnFixedUpdate(float fixedDeltaTime);
    }
}