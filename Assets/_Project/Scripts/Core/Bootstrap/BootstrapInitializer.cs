using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;

public class BootstrapInitializer : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // MessagePipe setup
        builder.RegisterMessagePipe();
        builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));

        // Register game services
        builder.Register<IGameService, GameService>(Lifetime.Singleton);
        builder.Register<AudioManager>(Lifetime.Singleton);

        builder.Register<ISceneController, SceneController>(Lifetime.Singleton);
    }

    void Start()
    {
        // Load Main Menu after setup
        SceneLoader.LoadSceneAsync("MainMenu").Forget();
    }
}
