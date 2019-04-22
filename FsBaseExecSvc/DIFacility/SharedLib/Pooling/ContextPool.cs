using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace DIFacility.SharedLib.Utils.Pooling
{

    class ContextPool<T> : IDisposable, IContextPool<T>
    {
        interface IItemStore
        {
            T Fetch();
            void Store(T item);
            int Count { get; }
        }
        public bool IsDisposed { get; private set; }
        private Func<ContextPool<T>, T> factory;
        private LoadingMode loadingMode;
        private IItemStore itemStore;
        private int size;
        private int count;
        private Semaphore sync;
        public ContextPool(int size, Func<ContextPool<T>, T> factory, LoadingMode loadingMode, AccessMode accessMode)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException("size", size,
                    "Argument 'size' must be greater than zero.");
            if (factory == null)
                throw new ArgumentNullException("factory");
            this.size = size;
            this.factory = factory;
            sync = new Semaphore(size, size);
            this.loadingMode = loadingMode;
            this.itemStore = CreateItemStore(accessMode, size);
            if (loadingMode == LoadingMode.Eager)
            {
                PreloadItems();
            }
        }
        #region Collection wrappers
        private IItemStore CreateItemStore(AccessMode mode, int capacity)
        {
            switch (mode)
            {
                case AccessMode.FIFO:
                    return new QueueStore(capacity);
                case AccessMode.LIFO:
                    return new StackQueue(capacity);
                default:
                    Debug.Assert(mode == AccessMode.Circular, "Invalid AccessMode in CreateItemStore");
                    return new CircularStore(capacity);
            }
        }

        class QueueStore : Queue<T>, IItemStore
        {
            public QueueStore(int capacity) : base(capacity) { }
            public T Fetch()
            {
                return this.Dequeue();
            }

            public void Store(T item)
            {
                this.Enqueue(item);
            }
        }

        class StackQueue : Stack<T>, IItemStore
        {
            public StackQueue(int capacity) : base(capacity) { }

            public T Fetch()
            {
                return this.Pop();
            }

            public void Store(T item)
            {
                this.Push(item);
            }
        }

        class CircularStore : IItemStore
        {
            private int position = -1;
            private int freeSlotCount = 0;
            private List<Slot> slots;

            public CircularStore(int capacity)
            {
                slots = new List<Slot>(capacity);
            }

            public int Count
            {
                get { return freeSlotCount; }
            }


            public T Fetch()
            {
                if (Count == 0)
                {
                    throw new InvalidOperationException("The buffer is empty.");
                }
                int startposition = position;
                do
                {
                    Advance();
                    Slot slot = slots[position];
                    if (!slot.InUse)
                    {
                        slot.InUse = true;
                        --freeSlotCount;
                        return slot.Item;
                    }
                } while (startposition != position);
                throw new InvalidOperationException("No free slots.");
            }
            private void Advance()
            {
                position = (position + 1) % slots.Count;
            }
            public void Store(T item)
            {
                Slot slot = this.slots.Find(s => object.ReferenceEquals(s, item));
                if (slot == null)
                {
                    slot = new Slot(item);
                    slots.Add(slot);
                }
                slot.InUse = false;
                ++freeSlotCount;
            }

            class Slot
            {
                public Slot(T item)
                {
                    this.Item = item;
                }

                public T Item { get; private set; }
                public bool InUse { get; set; } = false;
            }
        }
        #endregion

        public T Acquire()
        {
            sync.WaitOne();
            switch (loadingMode)
            {
                case LoadingMode.Eager:
                    return AcquireEager();
                case LoadingMode.Lazy:
                    return AcquireLazy();
                default:
                    Debug.Assert(loadingMode == LoadingMode.LazyExpanding,
                        "Unknown LoadingMode encountered in Acquire method.");
                    return AcquireLazyExpanding();
            }
        }
        //when Eager, this is called in constructor
        private void PreloadItems()
        {
            for (int i = 0; i < size; i++)
            {
                T item = factory(this);
                itemStore.Store(item);
            }
            count = size;
        }


        private T AcquireEager()
        {
            lock (itemStore)
            {
                return itemStore.Fetch();
            }
        }
        //when Lazy, we creates when no item availabe
        private T AcquireLazy()
        {
            lock (itemStore)
            {
                if (itemStore.Count > 0)
                {
                    return itemStore.Fetch();
                }
            }
            Interlocked.Increment(ref count);
            //it will be stored to the Collections on its dispose through this ref to the pool.
            return factory(this);
        }

        private T AcquireLazyExpanding()
        {
            bool shouldExpand = false;
            if (count < size)
            {
                int newCount = Interlocked.Increment(ref count);
                if (newCount <= size)
                {
                    shouldExpand = true;
                }
                else
                {
                    // Another thread took the last spot - use the store instead
                    Interlocked.Decrement(ref count);
                }
            }
            if (shouldExpand)
            {
                return factory(this);
            }
            else
            {
                lock (itemStore)
                {
                    return itemStore.Fetch();
                }
            }
        }


        public void Release(T item)
        {
            lock (itemStore)
            {
                itemStore.Store(item);
            }
            sync.Release();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
            {
                lock (itemStore)
                {
                    while (itemStore.Count > 0)
                    {
                        IDisposable disposable = (IDisposable)itemStore.Fetch();
                        disposable.Dispose();
                    }
                }
            }
            sync.Close();
        }
    }

}