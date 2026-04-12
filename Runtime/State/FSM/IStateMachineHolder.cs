namespace CFramework
{
    public interface IStateMachineHolder<TState>
    {
        IStateMachine<TState> StateMachine { get; protected internal set; }
    }
}