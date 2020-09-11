using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class CinemachineCameraManager : MonoSingleton<CinemachineCameraManager>
{
    [SerializeField]
    private PlayableDirector playableDirector;
    public void PlayFromTimeLine(ref TimelineAsset timeline)
    {
        playableDirector.Play(timeline);
    }
}
