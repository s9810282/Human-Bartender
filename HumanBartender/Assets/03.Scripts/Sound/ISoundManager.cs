
public interface ISoundManager
{
    // BG
    void PlayBGM(string name, float fadeDuration = 1f, bool loop = true);
    void StopBGM(float fadeDuration = 1f);
    void PauseBGM();
    void ResumeBGM();

    // SE (동시 재생)
    void PlaySE(string name);

    // Special SE (단독 채널)
    void PlaySpecialSE(string name, bool interrupt = true);
    void StopSpecialSE();
    bool IsSpecialSEPlaying { get; }

    // Volume (0~1)
    void SetBGMVolume(float v);
    void SetSEVolume(float v);
    void SetSpecialSEVolume(float v);
}
