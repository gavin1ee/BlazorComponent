﻿namespace BlazorComponent
{
    public interface ITreeview<TItem, TKey> : IHasProviderComponent
    {
        bool OpenAll { get; }

        void AddNode(ITreeviewNode<TItem, TKey> node);

        void UpdateActive(TKey key);

        void UpdateSelected(TKey key);

        void UpdateOpen(TKey key);

        bool IsSelected(TKey key);

        bool IsIndeterminate(TKey key);

        bool IsActive(TKey key);

        bool IsOpen(TKey key);

        bool IsExcluded(TKey key);

        Task EmitActiveAsync();

        Task EmitOpenAsync();
        Task EmitSelectedAsync();
    }
}
