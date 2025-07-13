// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using Unity.Entities;
using Unity.Mathematics;

public struct Timer : IComponentData
{
    public float Duration; // Total duration of the timer
    public float ElapsedTime; // Time elapsed since the timer started
}

public class TimerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        // Iterate over all entities with a Timer component
        Entities.ForEach((ref Timer timer) =>
        {
            // Update the elapsed time
            timer.ElapsedTime += deltaTime;

            // Check if the timer has completed
            if (timer.ElapsedTime >= timer.Duration)
            {
                // Reset the timer or perform an action here
                timer.ElapsedTime = 0;

                // You can trigger events or add additional logic here
            }
        }).ScheduleParallel();
    }
}