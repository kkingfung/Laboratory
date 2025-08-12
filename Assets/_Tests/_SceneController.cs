using UnityEngine;

namespace Presentation
{
    /// <summary>
    /// Controls scene lifecycle, initializes View and ViewModel.
    /// </summary>
    public class _SceneController : MonoBehaviour
    {
        [SerializeField] private ViewBase _view = null!;
        private ViewModelBase _viewModel = null!;

        private void Awake()
        {
            // Instantiate or resolve ViewModel, e.g. via ServiceLocator or DI container
            _viewModel = new SampleViewModel();

            // Link View with ViewModel
            _view.SetViewModel(_viewModel);

            // Initialize ViewModel if needed
            _viewModel.Initialize();
        }

        private void OnDestroy()
        {
            _viewModel.Dispose();
        }
    }
}
