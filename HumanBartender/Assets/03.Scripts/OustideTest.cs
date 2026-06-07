using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class OustideTest : MonoBehaviour
{

    [Header("Timeline")]
    [SerializeField] PlayableDirector director;


    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.T))
        {
            director.Play();
        }
    }

    public void PlayTimelineCutScene(TimelineAsset timeline)
    {
        if (director == null) return;

        director.playableAsset = timeline;
        director.time = 0;
        director.Play();
    }
}
