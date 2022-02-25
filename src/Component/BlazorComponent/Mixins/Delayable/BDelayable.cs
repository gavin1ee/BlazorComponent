using Microsoft.AspNetCore.Components;

namespace BlazorComponent;

public class BDelayable : BDomComponentBase, IDelayable
{
    private IDelayable _delayer;
    private int _openDelay;
    private bool _openDelayChanged = true;
    private int _closeDelay;
    private bool _closeDelayChanged = true;

    [Parameter]
    public virtual int OpenDelay
    {
        get
        {
            return _openDelay;
        }
        set
        {
            if (_openDelay == value) return;

            _openDelayChanged = true;
            _openDelay = value;
        }
    }

    [Parameter]
    public virtual int CloseDelay
    {
        get
        {
            return _closeDelay;
        }
        set
        {
            if (_closeDelay == value) return;

            _closeDelayChanged = true;
            _closeDelay = value;
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (_openDelayChanged || _closeDelayChanged)
        {
            _delayer = new Delayer(this);

            _openDelayChanged = false;
            _closeDelayChanged = false;
        }
    }

    public Task RunOpenDelayAsync(Func<Task> cb = null) => _delayer.RunOpenDelayAsync(cb);

    public Task RunCloseDelayAsync(Func<Task> cb = null) => _delayer.RunCloseDelayAsync(cb);
}