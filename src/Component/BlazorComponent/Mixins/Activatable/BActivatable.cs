using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BlazorComponent.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorComponent;

public abstract class BActivatable : BDelayable, IActivatable
{
    private readonly string _activatorId;
    private bool _isActive;
    private ElementReference? _externalActivatorRef;
    private Dictionary<string, EventCallback<FocusEventArgs>> _focusListeners = new();
    private Dictionary<string, EventCallback<KeyboardEventArgs>> _keyboardListeners = new();
    private Dictionary<string, (EventCallback<MouseEventArgs> listener, EventListenerActions actions)> _mouseListeners = new();

    protected BActivatable()
    {
        _activatorId = $"_activator_{Guid.NewGuid()}";
    }

    [Parameter]
    public RenderFragment<ActivatorProps> ActivatorContent { get; set; }

    [Parameter]
    public bool Disabled
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    [Parameter]
    public bool OpenOnHover { get; set; }

    [Parameter]
    public bool OpenOnFocus { get; set; }

    [Parameter]
    public virtual bool Value
    {
        get => IsActive;
        set => IsActive = value;
    }

    [Parameter]
    public EventCallback<bool> ValueChanged { get; set; }

    [Inject]
    public Document Document { get; set; }

    protected string InternalActivatorSelector => $"[{_activatorId}]";

    protected string ActivatorSelector => _externalActivatorRef.HasValue
        ? Document.GetElementByReference(_externalActivatorRef.Value).Selector
        : InternalActivatorSelector;

    protected HtmlElement ActivatorElement { get; private set; }

    protected virtual bool IsActive
    {
        get => _isActive;
        set
        {
            if (Disabled) return;

            _isActive = value;
        }
    }

    protected bool HasActivator => ActivatorContent != null || _externalActivatorRef != null;

    public RenderFragment ComputedActivatorContent
    {
        get
        {
            if (ActivatorContent != null)
            {
                var props = new ActivatorProps(GenActivatorAttributes());
                return ActivatorContent(props);
            }

            return null;
        }
    }

    protected virtual Dictionary<string, object> GenActivatorAttributes()
    {
        return new Dictionary<string, object>(ActivatorEvents)
        {
            {_activatorId, true},
            {"role", "button"},
            {"aria-haspopup", true},
            {"aria-expanded", IsActive}
        };
    }

    public Dictionary<string, object> ActivatorEvents { get; protected set; } = new();

    protected override void OnInitialized()
    {
        base.OnInitialized();

        Watcher
            .Watch<bool>(nameof(Disabled), val =>
            {
                ResetActivator();
            });

        ResetActivator();
    }

    private void ResetActivator()
    {
        RemoveActivatorEvents();
        ActivatorElement = null;
        GetActivator();
        AddActivatorEvents();
    }

    private void RemoveActivatorEvents()
    {
        if (ActivatorElement == null) return;

        //Empty events
        ActivatorEvents.Clear();

        _mouseListeners.Clear();
        _focusListeners.Clear();
        _keyboardListeners.Clear();
    }

    protected HtmlElement GetActivator()
    {
        if (ActivatorElement != null) return ActivatorElement;

        if (ActivatorContent != null)
        {
            ActivatorElement = Document.QuerySelector(InternalActivatorSelector);
        }
        else if (_externalActivatorRef != null)
        {
            ActivatorElement = Document.GetElementByReference(_externalActivatorRef.Value);
        }

        return ActivatorElement;
    }

    private void AddActivatorEvents()
    {
        if (Disabled || GetActivator() == null) return;

        _mouseListeners = GenActivatorMouseListeners();
        _focusListeners = GenActivatorFocusListeners();
        _keyboardListeners = GenActivatorKeyboardListeners();

        foreach (var (key, (listener, actions)) in _mouseListeners)
        {
            ActivatorEvents.Add(key, listener);

            if (actions != null)
            {
                if (actions.StopPropagation)
                {
                    ActivatorEvents.Add("__internal_stopPropagation_" + key, true);
                }

                if (actions.PreventDefault)
                {
                    ActivatorEvents.Add("__internal_preventDefault_" + key, true);
                }
            }
        }

        foreach (var (key, listener) in _focusListeners)
        {
            ActivatorEvents.Add(key, listener);
        }

        foreach (var (key, listener) in _keyboardListeners)
        {
            ActivatorEvents.Add(key, listener);
        }
    }

    protected virtual Dictionary<string, EventCallback<FocusEventArgs>> GenActivatorFocusListeners()
    {
        Dictionary<string, EventCallback<FocusEventArgs>> listeners = new();

        if (Disabled || !OpenOnFocus) return listeners;

        listeners.Add("onexfocus", CreateEventCallback<FocusEventArgs>(_ => Open()));

        listeners.Add("onexblur", CreateEventCallback<FocusEventArgs>(_ => Close()));

        return listeners;
    }

    protected virtual async Task Open()
    {
        await RunOpenDelayAsync(() => UpdateValue(true));
    }

    protected async Task UpdateValue(bool value)
    {
        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(value);
        }
        else
        {
            Value = value;
        }
    }

    protected virtual async Task Close()
    {
        await RunCloseDelayAsync(() => UpdateValue(false));
    }

    protected virtual Dictionary<string, (EventCallback<MouseEventArgs> listener, EventListenerActions actions)> GenActivatorMouseListeners()
    {
        Dictionary<string, (EventCallback<MouseEventArgs>, EventListenerActions)> listeners = new();

        if (Disabled) return listeners;

        if (OpenOnHover)
        {
            listeners.Add("onexmouseenter", (CreateEventCallback<MouseEventArgs>(_ => Open()), null));
            listeners.Add("onexmouseleave", (CreateEventCallback<MouseEventArgs>(_ => Close()), null));
        }
        else
        {
            listeners.Add("onexclick", (CreateEventCallback<MouseEventArgs>(async _ =>
            {
                await JsInvokeAsync(JsInteropConstants.Focus, ActivatorSelector);
                await Toggle();
            }), new EventListenerActions(true)));
        }

        return listeners;
    }

    protected virtual Task Toggle()
    {
        return UpdateValue(!Value);
    }

    protected virtual Dictionary<string, EventCallback<KeyboardEventArgs>> GenActivatorKeyboardListeners()
    {
        Dictionary<string, EventCallback<KeyboardEventArgs>> listeners = new();

        if (Disabled) return listeners;

        listeners.Add("onexkeydown", CreateEventCallback<KeyboardEventArgs>(async args =>
        {
            if (args.Key == "Escape")
            {
                await Close();
            }
        }));

        return listeners;
    }

    /// <summary>
    /// Use to set activator
    /// </summary>
    /// <param name="el"></param>
    /// <returns></returns>
    public void UpdateActivator(ElementReference el)
    {
        _externalActivatorRef = el;
        ResetActivator();
    }
}