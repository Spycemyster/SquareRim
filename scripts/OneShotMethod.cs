using System;
using System.Collections.Generic;
using Godot;

public class OneShotMethod<T> : Node
{
    private Timer mTimer;
    private Action<T[]> mAction;
    private Node mNode;
    private T[] mParams;
    private float mSeconds;
    private Node mCaller;
    public OneShotMethod(Action<T[]> action, float seconds, Node obj, params T[] a)
    {
        mCaller = obj;
        mSeconds = seconds;
        mParams = a;
        mAction = action;
    }

    public void Start()
    {
        mTimer = new Timer();
        mTimer.OneShot = true;
        mTimer.Connect("timeout", this, "onTimer");
        mCaller.AddChild(mTimer);
        mTimer.Start(mSeconds);
    }

    private void onTimer()
    {
        mAction.Invoke(mParams);
        mTimer.QueueFree();
        QueueFree();
    }
}