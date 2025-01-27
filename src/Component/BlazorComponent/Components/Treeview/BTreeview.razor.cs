﻿using Microsoft.AspNetCore.Components;

namespace BlazorComponent
{
    public partial class BTreeview<TItem, TKey> : ITreeview<TItem, TKey>
    {
        private List<TItem> _oldItems;
        private string _oldItemsKeys;
        private List<TKey> _oldValue;
        private List<TKey> _oldActive;
        private List<TKey> _oldOpen;

        [Parameter]
        public List<TItem> Items { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public string Search { get; set; }

        [Parameter]
        public RenderFragment<TreeviewItem<TItem>> PrependContent { get; set; }

        [Parameter]
        public RenderFragment<TreeviewItem<TItem>> LabelContent { get; set; }

        [EditorRequired]
        [Parameter]
        public Func<TItem, string> ItemText { get; set; }

        [Parameter]
        public bool Selectable { get; set; }

        [Parameter]
        public bool OpenAll { get; set; }

        [Parameter]
        public Func<TItem, bool> ItemDisabled { get; set; }

        [EditorRequired]
        [Parameter]
        public Func<TItem, TKey> ItemKey { get; set; }

        [EditorRequired]
        [Parameter]
        public Func<TItem, List<TItem>> ItemChildren { get; set; }

        [Parameter]
        public SelectionType SelectionType { get; set; }

        [Parameter]
        public List<TKey> Value { get; set; }

        [Parameter]
        public EventCallback<List<TKey>> ValueChanged { get; set; }

        [Parameter]
        public bool MultipleActive { get; set; }

        [Parameter]
        public List<TKey> Active { get; set; }

        [Parameter]
        public EventCallback<List<TKey>> ActiveChanged { get; set; }

        [Parameter]
        public List<TKey> Open { get; set; }

        [Parameter]
        public EventCallback<List<TKey>> OpenChanged { get; set; }

        [Parameter]
        public EventCallback<List<TItem>> OnInput { get; set; }

        [Parameter]
        public EventCallback<List<TItem>> OnActiveUpdate { get; set; }

        [Parameter]
        public EventCallback<List<TItem>> OnOpenUpdate { get; set; }

        [Parameter]
        public bool Dark { get; set; }

        [Parameter]
        public bool Light { get; set; }

        [CascadingParameter(Name = "IsDark")]
        public bool CascadingIsDark { get; set; }

        public bool IsDark
        {
            get
            {
                if (Dark)
                {
                    return true;
                }

                if (Light)
                {
                    return false;
                }

                return CascadingIsDark;
            }
        }

        public List<TItem> ComputedItems
        {
            get
            {
                if (string.IsNullOrEmpty(Search))
                {
                    return Items;
                }
                else
                {
                    return Items.Where(r => !IsExcluded(ItemKey(r))).ToList();
                }
            }
        }

        public RenderFragment AppendContent { get; }

        //TODO:
        public bool IsLoading { get; }

        [Parameter]
        public string LoadingIcon { get; set; } = "mdi-cached";

        [Parameter]
        public string ExpandIcon { get; set; } = "mdi-menu-down";

        public TKey ActiveKey { get; private set; }

        public Dictionary<TKey, NodeState<TItem, TKey>> Nodes { get; private set; } = new();

        public List<TKey> ExcludedItems
        {
            get
            {
                var excluded = new List<TKey>();

                if (string.IsNullOrEmpty(Search))
                {
                    return excluded;
                }

                foreach (var item in Items)
                {
                    FilterTreeItems(item, ref excluded);
                }

                return excluded;
            }
        }

        public void AddNode(ITreeviewNode<TItem, TKey> node)
        {
            if (Nodes.TryGetValue(node.Key, out var nodeState))
            {
                nodeState.Node = node;
            }
        }

        public bool IsActive(TKey key)
        {
            if (Nodes.TryGetValue(key, out var nodeState))
            {
                return nodeState.IsActive;
            }

            return false;
        }

        public bool IsIndeterminate(TKey key)
        {
            if (Nodes.TryGetValue(key, out var nodeState))
            {
                return nodeState.IsIndeterminate;
            }

            return false;
        }

        public bool IsOpen(TKey key)
        {
            if (Nodes.TryGetValue(key, out var nodeState))
            {
                return nodeState.IsOpen;
            }

            return false;
        }

        public bool IsSelected(TKey key)
        {
            if (Nodes.TryGetValue(key, out var nodeState))
            {
                return nodeState.IsSelected;
            }

            return false;
        }

        public void UpdateActive(TKey key)
        {
            if (Nodes.TryGetValue(key, out var nodeState))
            {
                if (MultipleActive)
                {
                    nodeState.IsActive = !nodeState.IsActive;
                }
                else
                {
                    if (!nodeState.IsActive)
                    {
                        nodeState.IsActive = true;

                        //only one active
                        foreach (var otherNodeState in Nodes.Values.Where(r => r != nodeState && r.IsActive))
                        {
                            otherNodeState.IsActive = false;
                        }
                    }
                    else
                    {
                        nodeState.IsActive = false;
                    }
                }
            }
        }

        public async Task EmitActiveAsync()
        {
            _oldActive = Nodes.Values.Where(r => r.IsActive).Select(r => ItemKey(r.Item)).ToList();
            if (ListComparer.Equals(_oldActive, Active))
            {
                //nothing change
                return;
            }

            Active = _oldActive;

            if (OnActiveUpdate.HasDelegate)
            {
                var active = Nodes.Values.Where(r => r.IsActive).Select(r => r.Item).ToList();
                _ = OnActiveUpdate.InvokeAsync(active);
            }

            if (ActiveChanged.HasDelegate)
            {
                await ActiveChanged.InvokeAsync(Active);
            }
            else
            {
                StateHasChanged();
            }
        }

        public void UpdateOpen(TKey key)
        {
            if (Nodes.TryGetValue(key, out var nodeState))
            {
                nodeState.IsOpen = !nodeState.IsOpen;
            }
        }

        public async Task EmitOpenAsync()
        {
            _oldOpen = Nodes.Values.Where(r => r.IsOpen).Select(r => ItemKey(r.Item)).ToList();
            if (ListComparer.Equals(_oldOpen, Open))
            {
                //nothing change
                return;
            }

            Open = _oldOpen;

            if (OnOpenUpdate.HasDelegate)
            {
                var open = Nodes.Values.Where(r => r.IsOpen).Select(r => r.Item).ToList();
                _ = OnOpenUpdate.InvokeAsync(open);
            }

            if (OpenChanged.HasDelegate)
            {
                await OpenChanged.InvokeAsync(Open);
            }
        }

        public void UpdateSelected(TKey key)
        {
            UpdateSelected(key, null);
        }

        public void UpdateSelected(TKey key, bool? isSelected)
        {
            if (Nodes.TryGetValue(key, out var nodeState))
            {
                nodeState.IsSelected = isSelected ?? !nodeState.IsSelected;
                nodeState.IsIndeterminate = false;

                if (SelectionType == SelectionType.Leaf)
                {
                    UpdateChildrenSelected(nodeState.Children, nodeState.IsSelected);
                    UpdateParentSelected(nodeState.Parent);
                }
            }
        }

        public async Task EmitSelectedAsync()
        {
            _oldValue = Nodes.Values.Where(r => r.IsSelected).Select(r => ItemKey(r.Item)).ToList();
            if (ListComparer.Equals(_oldValue, Value))
            {
                //nothing change
                return;
            }

            Value = _oldValue;

            if (ValueChanged.HasDelegate)
            {
                await ValueChanged.InvokeAsync(Value);
            }
            else if (OnInput.HasDelegate)
            {
                var value = Nodes.Values.Where(r => r.IsSelected).Select(r => r.Item).ToList();
                await OnInput.InvokeAsync(value);
            }
            else
            {
                StateHasChanged();
            }
        }

        /// <summary>
        /// Rebuilds the tree with current Items.
        /// </summary>
        public void RebuildTree()
        {
            BuildTree(Items, default);
        }

        private void UpdateParentSelected(TKey parent, bool isIndeterminate = false)
        {
            if (parent == null)
            {
                return;
            }

            if (Nodes.TryGetValue(parent, out var nodeState))
            {
                if (isIndeterminate)
                {
                    nodeState.IsIndeterminate = true;
                }
                else
                {
                    var children = Nodes
                                   .Where(r => nodeState.Children.Contains(r.Key)).Select(r => r.Value);
                    if (children.All(r => r.IsSelected))
                    {
                        nodeState.IsSelected = true;
                        nodeState.IsIndeterminate = false;
                    }
                    else if (children.All(r => !r.IsSelected))
                    {
                        nodeState.IsSelected = false;
                        nodeState.IsIndeterminate = false;
                    }
                    else
                    {
                        nodeState.IsIndeterminate = true;
                    }
                }

                UpdateParentSelected(nodeState.Parent, nodeState.IsIndeterminate);
            }
        }

        private void UpdateChildrenSelected(IEnumerable<TKey> children, bool isSelected)
        {
            foreach (var child in children)
            {
                if (Nodes.TryGetValue(child, out var nodeState))
                {
                    //control by parent
                    nodeState.IsSelected = isSelected;
                    UpdateChildrenSelected(nodeState.Children, isSelected);
                }
            }
        }

        private bool FilterTreeItems(TItem item, ref List<TKey> excluded)
        {
            if (FilterTreeItem(item, Search, ItemText))
            {
                return true;
            }

            var children = ItemChildren(item);

            if (children != null)
            {
                var match = false;

                foreach (var child in children)
                {
                    if (FilterTreeItems(child, ref excluded))
                    {
                        match = true;
                    }
                }

                if (match)
                {
                    return true;
                }
            }

            excluded.Add(ItemKey(item));
            return false;
        }

        private bool FilterTreeItem(TItem item, string search, Func<TItem, string> itemText)
        {
            var text = itemText(item).ToString();
            return text.ToLower().IndexOf(search.ToLower()) > -1;
        }

        public bool IsExcluded(TKey key)
        {
            return !string.IsNullOrEmpty(Search) && ExcludedItems.Contains(key);
        }

        private string CombineItemKeys(IList<TItem> list)
        {
            string keys = string.Empty;

            if (list == null || list.Count == 0)
            {
                return keys;
            }

            foreach (var item in list)
            {
                var key = ItemKey(item);
                keys += key.ToString();

                var children = ItemChildren(item);
                if (children is not null)
                {
                    keys += CombineItemKeys(children);
                }
            }

            return keys;
        }

        protected override void OnParametersSet()
        {
            if (_oldItems != Items)
            {
                _oldItems = Items;
                _oldItemsKeys = CombineItemKeys(Items);

                BuildTree(Items, default);
            }
            else
            {
                var itemKeys = CombineItemKeys(Items);
                if (_oldItemsKeys != itemKeys)
                {
                    _oldItemsKeys = itemKeys;

                    BuildTree(Items, default);
                }
            }

            if (!ListComparer.Equals(_oldValue, Value))
            {
                UpdateSelected();
                _oldValue = Value;
            }

            if (!ListComparer.Equals(_oldActive, Active))
            {
                UpdateActive();
                _oldActive = Active;
            }

            if (!ListComparer.Equals(_oldOpen, Open))
            {
                UpdateOpen();
                _oldOpen = Open;
            }
        }

        private void UpdateOpen()
        {
            if (Open == null || !Open.Any())
            {
                return;
            }

            foreach (var nodeState in Nodes.Values)
            {
                var key = ItemKey(nodeState.Item);
                if (Open.Contains(key))
                {
                    nodeState.IsOpen = true;
                }
                else
                {
                    nodeState.IsOpen = false;
                }
            }
        }

        private void UpdateActive()
        {
            if (Active == null || !Active.Any())
            {
                return;
            }

            var hasActive = false;
            foreach (var nodeState in Nodes.Values)
            {
                var key = ItemKey(nodeState.Item);
                if (Active.Contains(key) && (MultipleActive || !hasActive))
                {
                    nodeState.IsActive = true;
                    hasActive = true;
                }
                else
                {
                    nodeState.IsActive = false;
                }
            }
        }

        private void UpdateSelected()
        {
            if (Value == null || !Value.Any())
            {
                return;
            }

            //clear all selected
            foreach (var selectedNodeState in Nodes.Values.Where(r => r.IsSelected))
            {
                selectedNodeState.IsSelected = false;
            }

            foreach (var key in Value)
            {
                UpdateSelected(key, true);
            }
        }

        private void BuildTree(List<TItem> items, TKey parent)
        {
            foreach (var item in items)
            {
                var key = ItemKey(item);
                var children = ItemChildren(item) ?? new List<TItem>();

                if (!Nodes.TryGetValue(key, out var oldNode))
                {
                    oldNode = new NodeState<TItem, TKey>();
                }

                var newNode = new NodeState<TItem, TKey>
                {
                    Node = oldNode.Node,
                    Parent = parent,
                    Children = children.Select(ItemKey),
                    Item = item,
                };

                BuildTree(children, key);

                newNode.IsActive = oldNode.IsActive;
                newNode.IsOpen = oldNode.IsOpen;

                Nodes[key] = newNode;
            }
        }
    }
}
