using UnityEngine;
using UnityEngine.Rendering.Universal;
using VContainer.Unity;

public interface IDisplaySettings
{
    Vector2Int[] WindowedSizes { get; }
    void SetFullscreen(bool on);
    void SetWindowedSize(int index);
    void SetFillMode(bool stretchToFill);

    void RegisterActiveCamera(PixelPerfectCamera ppc);
    void UnregisterActiveCamera(PixelPerfectCamera ppc);
}

public class DisplaySettings : IDisplaySettings, IInitializable
{
    private static readonly Vector2Int[] _windowedSizes =
    {
        new(1280, 720),
        new(1600, 900),
        new(1920, 1080),
    };

    public Vector2Int[] WindowedSizes => _windowedSizes;

    private PixelPerfectCamera _activePPC;

    public void Initialize()
    {
        bool full = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        SetFullscreen(full);
    }

    public void SetFullscreen(bool on)
    {
        if (on)
        {
            var r = Screen.currentResolution;
            Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            int i = Mathf.Clamp(PlayerPrefs.GetInt("WinIndex", 0), 0, _windowedSizes.Length - 1);
            var s = _windowedSizes[i];
            Screen.SetResolution(s.x, s.y, FullScreenMode.Windowed);
        }
        PlayerPrefs.SetInt("Fullscreen", on ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetWindowedSize(int index)
    {
        index = Mathf.Clamp(index, 0, _windowedSizes.Length - 1);
        PlayerPrefs.SetInt("WinIndex", index);
        PlayerPrefs.Save();
        if (!Screen.fullScreen)
        {
            var s = _windowedSizes[index];
            Screen.SetResolution(s.x, s.y, FullScreenMode.Windowed);
        }
    }

    public void SetFillMode(bool stretchToFill)
    {
        PlayerPrefs.SetInt("FillStretch", stretchToFill ? 1 : 0);
        PlayerPrefs.Save();
        ApplyFillToActive();   // 등록된 PPC에 즉시 반영
    }

    public void RegisterActiveCamera(PixelPerfectCamera ppc)
    {
        if (ppc == null) return;
        _activePPC = ppc;
        ApplyFillToActive();   // 씬 진입 시 새 PPC에 현재 설정 반영
    }

    public void UnregisterActiveCamera(PixelPerfectCamera ppc)
    {
        // 다른 PPC가 이미 등록됐다면 덮어쓰지 않음
        if (_activePPC == ppc) _activePPC = null;
    }

    private void ApplyFillToActive()
    {
        if (_activePPC == null) return;
        bool stretch = PlayerPrefs.GetInt("FillStretch", 1) == 1;
        _activePPC.cropFrame = stretch
            ? PixelPerfectCamera.CropFrame.StretchFill
            : PixelPerfectCamera.CropFrame.Windowbox;
    }
}