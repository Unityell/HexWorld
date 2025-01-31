public class UnitActionsWidget : Widgets
{
    Unit Unit;

    void Awake() => EventBus.Subscribe<PickUnitSignal>(SignalBox);

    protected override void SignalBox<T>(T Obj)
    {
        switch (Obj)
        {
            case PickUnitSignal PickUnitSignal :
                Enable(true);
                Unit = PickUnitSignal.Unit;
                break;
            default: break;
        }
    }

    public void Jump()
    {
        EventBus.Invoke(new UnitJumpSignal(Unit));
    }

    public void Walk()
    {
        EventBus.Invoke(new UnitWalkSignal(Unit));
    }

    void OnDestroy() => EventBus.Unsubscribe<PickUnitSignal>(SignalBox);
}