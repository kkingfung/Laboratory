using System;
using UnityEngine;

namespace Presentation
{
    public class TemplateView : MonoBehaviour, IDisposable
    {
        protected _ViewModel? ViewModel { get; private set; }

        /// <summary>
        /// Set the ViewModel for this view and bind UI events.
        /// </summary>
        public virtual void SetViewModel(_ViewModel viewModel)
        {
            ViewModel = viewModel;
            Bind();
        }

        /// <summary>
        /// Bind View UI elements to ViewModel observables or commands.
        /// Override in derived classes.
        /// </summary>
        protected virtual void Bind()
        { 
            // Cleanup if needed
        }

        public virtual void Dispose()
        {
            // Cleanup if needed
        }
    }
}
