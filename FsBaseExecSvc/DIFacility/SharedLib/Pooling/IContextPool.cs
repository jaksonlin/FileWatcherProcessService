namespace DIFacility.SharedLib.Utils.Pooling
{
    interface IContextPool<T>
    {
        bool IsDisposed { get; }

        T Acquire();
        void Release(T item);
    }
}