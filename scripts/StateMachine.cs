using Godot;

public class StateMachine
{
    private State mCurrState = null;

    /// <summary>
    /// Sets the state.
    /// </summary>
    /// <param name="state"></param>
    public void SetState(State state)
    {
        mCurrState = state;
        mCurrState.Initialize();
    }

    public void Process(float delta)
    {
        mCurrState?._Process(delta);
    }

    public void PhysicsProcess(float delta)
    {
        mCurrState?._ProcessPhysics(delta);
    }
}

public interface State
{
    void Initialize();
    void _Process(float dt);
    void _ProcessPhysics(float dt);
}