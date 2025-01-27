﻿using BlazorComponent.Abstracts;
using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace BlazorComponent
{
    public abstract class BDomComponentBase : BComponentBase, IHasProviderComponent
    {
        public BDomComponentBase()
        {
            CssProvider.StaticClass = () => Class;
            CssProvider.StaticStyle = () => Style;
            Watcher = new PropertyWatcher(GetType());
            OnWatcherInitialized();
        }

        [Inject]
        public IComponentIdGenerator ComponentIdGenerator { get; set; }

        [Parameter]
        public string Id { get; set; }

        /// <summary>
        /// Specifies one or more class names for an DOM element.
        /// </summary>
        [Parameter]
        public string Class { get; set; }

        /// <summary>
        /// Specifies an inline style for an DOM element.
        /// </summary>
        [Parameter]
        public string Style { get; set; }

        /// <summary>
        /// Custom attributes
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public virtual IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        protected const int BROWSER_RENDER_INTERVAL = 16;

        private ElementReference _ref;
        private ElementReference? _prevRef;
        private bool _elementReferenceChanged;

        public ComponentCssProvider CssProvider { get; } = new();

        public ComponentAbstractProvider AbstractProvider { get; } = new();

        public PropertyWatcher Watcher { get; }

        /// <summary>
        /// Returned ElementRef reference for DOM element.
        /// </summary>
        public virtual ElementReference Ref
        {
            get => _ref;
            set
            {
                if (_prevRef.HasValue)
                {
                    if (_prevRef.Value.Id != value.Id)
                    {
                        _prevRef = value;
                        _elementReferenceChanged = true;
                    }
                }
                else
                {
                    _prevRef = value;
                }

                _ref = value;
                RefBack?.Set(value);
            }
        }

        protected virtual void OnWatcherInitialized()
        {
        }

        protected override void OnInitialized()
        {
            Id ??= ComponentIdGenerator.Generate(this);
            base.OnInitialized();
        }

        protected override Task OnInitializedAsync()
        {
            SetComponentClass();
            return base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (_elementReferenceChanged)
            {
                _elementReferenceChanged = false;
                await OnElementReferenceChangedAsync();
            }
        }

        protected virtual Task OnElementReferenceChangedAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void SetComponentClass()
        {
        }

        protected TValue GetValue<TValue>(TValue @default = default, [CallerMemberName] string name = null)
        {
            return Watcher.GetValue(@default, name);
        }

        protected TValue GetComputedValue<TValue>(Expression<Func<TValue>> valueExpression, [CallerMemberName] string name = null)
        {
            return Watcher.GetComputedValue(valueExpression, name);
        }

        protected TValue GetComputedValue<TValue>(Func<TValue> valueFactory, string[] dependencyProperties, [CallerMemberName] string name = null)
        {
            return Watcher.GetComputedValue(valueFactory, dependencyProperties, name);
        }

        protected void SetValue<TValue>(TValue value, [CallerMemberName] string name = null)
        {
            Watcher.SetValue(value, name);
        }

        protected RenderFragment Render(Type type, Action<AttributesBuilder> parametersBuilderAction = null, object key = null, object data = null, Action<object> referenceCapture = null)
        {
            var metadata = AbstractProvider.GetMetadata(type, data);
            return builder =>
            {
                var sequence = 0;
                builder.OpenComponent(sequence++, metadata.Type);

                builder.AddMultipleAttributes(sequence++, metadata.Attributes);

                if (parametersBuilderAction != null)
                {
                    var parametersBuilder = new AttributesBuilder();
                    parametersBuilderAction.Invoke(parametersBuilder);

                    builder.AddMultipleAttributes(sequence++, parametersBuilder.Attributes);
                }

                if (key != null)
                {
                    builder.SetKey(key);
                }

                if (referenceCapture != null)
                {
                    builder.AddComponentReferenceCapture(sequence++, referenceCapture);
                }

                builder.CloseComponent();
            };
        }

        protected RenderFragment RenderPart(Type keyType)
        {
            return AbstractProvider.GetPartContent(keyType, this);
        }

        protected RenderFragment RenderPart(Type keyType, Action<AttributesBuilder> builderAction)
        {
            return AbstractProvider.GetPartContent(keyType, this, builderAction);
        }

        protected RenderFragment RenderPart(Type keyType, object arg0, [CallerArgumentExpression("arg0")] string arg0Name = null)
        {
            return AbstractProvider.GetPartContent(keyType, this, builder =>
            {
                builder
                    .Add(arg0Name, arg0);
            });
        }

        protected RenderFragment RenderPart(Type keyType, object arg0, object arg1, [CallerArgumentExpression("arg0")] string arg0Name = null, [CallerArgumentExpression("arg1")] string arg1Name = null)
        {
            return AbstractProvider.GetPartContent(keyType, this, builder =>
            {
                builder
                    .Add(arg0Name, arg0)
                    .Add(arg1Name, arg1);
            });
        }

        protected RenderFragment RenderPart(Type keyType, object arg0, object arg1, object arg2, [CallerArgumentExpression("arg0")] string arg0Name = null, [CallerArgumentExpression("arg1")] string arg1Name = null, [CallerArgumentExpression("arg2")] string arg2Name = null)
        {
            return AbstractProvider.GetPartContent(keyType, this, builder =>
            {
                builder
                    .Add(arg0Name, arg0)
                    .Add(arg1Name, arg1)
                    .Add(arg2Name, arg2);
            });
        }

        protected Dictionary<string, object> GetAttributes(Type type, object data = null)
        {
            return AbstractProvider.GetMetadata(type, data).Attributes;
        }

        protected Dictionary<string, object> GetAttributes(Type type, string name, object data = null)
        {
            return AbstractProvider.GetMetadata(type, name, data).Attributes;
        }

        public EventCallback<TValue> CreateEventCallback<TValue>(Func<TValue, Task> callback)
        {
            return EventCallback.Factory.Create(this, callback);
        }

        public EventCallback CreateEventCallback(Func<Task> callback)
        {
            return EventCallback.Factory.Create(this, callback);
        }

        public EventCallback<TValue> CreateEventCallback<TValue>(Action<TValue> callback)
        {
            return EventCallback.Factory.Create(this, callback);
        }

        public EventCallback CreateEventCallback(Action callback)
        {
            return EventCallback.Factory.Create(this, callback);
        }
    }
}