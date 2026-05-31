using System.Media;

namespace survival_list_overlay.Services;

public interface IAudioCueService
{
    void PlayItemCompleted();
}

public sealed class SystemAudioCueService : IAudioCueService
{
    public void PlayItemCompleted() => SystemSounds.Exclamation.Play();
}
