using UnityEngine;

public class ActorPlayback : MonoBehaviour
{
    [SerializeField] private bool play = false;
    [SerializeField] private bool loop = false;

    private FrameData[] frames;
    private int currentFrame = 0;

    public void SetFrames(FrameData[] newFrames)
    {
        frames = newFrames;
        currentFrame = 0;
    }

    public void Play() => play = true;
    public void Pause() => play = false;
    public void Stop()
    {
        play = false;
        currentFrame = 0;
    }

    public void GoToFrame(int frameIndex)
    {
        currentFrame = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
        ApplyFrame();
    }

    void FixedUpdate()
    {
        if (!play || frames == null || frames.Length == 0) return;

        ApplyFrame();

        currentFrame++;
        if (currentFrame >= frames.Length)
        {
            if (loop) currentFrame = 0;
            else play = false;
        }
    }

    private void ApplyFrame()
    {
        if (frames == null || frames.Length == 0) return;
        var f = frames[currentFrame];
        transform.position = f.position;
        transform.rotation = f.rotation;
    }
}
