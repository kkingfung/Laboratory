using System;
using UnityEngine;

namespace Presentation
{
    public class _View : MonoBehaviour, IDisposable
    {
        protected ViewModelBase? ViewModel { get; private set; }

        /// <summary>
        /// Set the ViewModel for this view and bind UI events.
        /// </summary>
        public virtual void SetViewModel(ViewModelBase viewModel)
        {
            ViewModel = viewModel;
            Bind();
        }

        /// <summary>
        /// Bind View UI elements to ViewModel observables or commands.
        /// Override in derived classes.
        /// </summary>
        protected abstract void Bind();

        public virtual void Dispose()
        {
            // Cleanup if needed
        }
    }
}
