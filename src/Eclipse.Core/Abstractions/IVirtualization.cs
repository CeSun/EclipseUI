using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Eclipse.Core.Abstractions
{
    public interface IVirtualizingContainer : IComponent
    {
        double ViewportSize { get; set; }
        double ItemSize { get; set; }
        int BufferSize { get; set; }
        double ScrollOffset { get; set; }
        (int StartIndex, int Count) GetVisibleRange();
        RenderFragment<object>? ItemTemplate { get; set; }
        event EventHandler<ScrollEventArgs>? ScrollChanged;
    }

    public sealed class ScrollEventArgs : EventArgs
    {
        public double Offset { get; }
        public double ViewportSize { get; }
        public double ExtentSize { get; }
        public ScrollEventArgs(double offset, double viewportSize, double extentSize)
        {
            Offset = offset; ViewportSize = viewportSize; ExtentSize = extentSize;
        }
    }

    public interface IVirtualizingItemsControl : IComponent
    {
        object? ItemsSource { get; set; }
        RenderFragment<object>? ItemTemplate { get; set; }
        double ItemSize { get; set; }
        bool IsVirtualizing { get; set; }
    }

    public interface IIncrementalSource<T>
    {
        int Count { get; }
        Task<IReadOnlyList<T>> GetRangeAsync(int startIndex, int count);
        event EventHandler<IncrementalSourceChangedEventArgs>? SourceChanged;
    }

    public sealed class IncrementalSourceChangedEventArgs : EventArgs
    {
        public NotifyCollectionChangedAction Action { get; }
        public int NewStartingIndex { get; }
        public int OldStartingIndex { get; }
        public int NewItemsCount { get; }
        public int OldItemsCount { get; }
        public static IncrementalSourceChangedEventArgs Reset() => new(NotifyCollectionChangedAction.Reset, 0, 0, 0, 0);
        public static IncrementalSourceChangedEventArgs Add(int index, int count) => new(NotifyCollectionChangedAction.Add, index, 0, count, 0);
        public static IncrementalSourceChangedEventArgs Remove(int index, int count) => new(NotifyCollectionChangedAction.Remove, 0, index, 0, count);
        public static IncrementalSourceChangedEventArgs Move(int oldIndex, int newIndex, int count) => new(NotifyCollectionChangedAction.Move, newIndex, oldIndex, count, count);
        private IncrementalSourceChangedEventArgs(NotifyCollectionChangedAction action, int newStartingIndex, int oldStartingIndex, int newItemsCount, int oldItemsCount)
        {
            Action = action; NewStartingIndex = newStartingIndex; OldStartingIndex = oldStartingIndex; NewItemsCount = newItemsCount; OldItemsCount = oldItemsCount;
        }
    }

    public interface IKeyProvider<T>
    {
        object GetKey(T item);
    }
}
