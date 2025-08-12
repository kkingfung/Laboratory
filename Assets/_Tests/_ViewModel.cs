using System;
using UniRx;

namespace Presentation
{
    public class _ViewModel : IDisposable
    {
        protected readonly CompositeDisposable Disposables = new();

        /// <summary>
        /// Called once when ViewModel is initialized.
        /// </summary>
        public virtual void Initialize()
        {
            // Override in derived classes for setup
        }

        public virtual void Dispose()
        {
            Disposables.Dispose();
        }
    }
}
