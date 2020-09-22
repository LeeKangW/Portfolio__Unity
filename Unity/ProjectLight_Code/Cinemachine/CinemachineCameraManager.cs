using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SingleTon
{
    public class CinemachineCameraManager : MonoBehaviour
    {
        [Header("Virtual Cam")]
        /** 가상 카메라 */
        [Tooltip("Original VCam")]
        [SerializeField] private CinemachineVirtualCamera vCam = null;
        [SerializeField] private PlayableDirector playableDirector = null;

        [Header("Debuging Field")]
        [SerializeField] private ENUM_VCam.WhatSystem whatSystem = ENUM_VCam.WhatSystem.Default;
        [SerializeField] private Scriptableobjects.VCamEvent Event = null;

        #region SingleTon
        private static CinemachineCameraManager instance;

        public static CinemachineCameraManager Instance
        {
            get { return instance; }
        }
        #endregion

        private void Awake()
        {
            if (instance == null)
            {
                playableDirector = this.GetComponent<PlayableDirector>();
                instance = this;

            }
            else
                Destroy(this.gameObject);
        }

        /// <summary>
        /// VCamEvent를 불러주는 함수
        /// </summary>
        /// <param name="vCamEvent"></param>
        public void StartVCamSystem(ref Scriptableobjects.VCamEvent vCamEvent)
        {
            whatSystem = vCamEvent.ENUM_whatSystem;

            switch (whatSystem)
            {
                case ENUM_VCam.WhatSystem.WS_ZoomIn:
                    ZoomIn(vCamEvent.ZoomSize);
                    break;

                case ENUM_VCam.WhatSystem.WS_ZoomOut:
                    ZoomOut(vCamEvent.OriginCamSize);
                    break;

                case ENUM_VCam.WhatSystem.WS_TimeLine:
                    PlayFromTimeLine(vCamEvent.Timeline);

                    break;

                default:
                    break;
            }
        }

        private void PlayFromTimeLine(TimelineAsset timeline)
        {
            if (timeline)
            {
                playableDirector.Play(timeline);
            }
        }

        #region Zoom Systems

        /// <summary>
        /// 줌인 시스템 용
        /// </summary>
        /// <param name="zoomsize"> zoomin 사이즈 </param>
        private void ZoomIn(float zoomsize)
        {
            StartCoroutine(COR_ZoomInOut(zoomsize, true));
        }

        /// <summary>
        /// 줌 아웃 시스템 용
        /// </summary>
        /// <param name="zoomsize"> zoomin 사이즈 </param>
        private void ZoomOut(float zoomsize)
        {
            StartCoroutine(COR_ZoomInOut(zoomsize, false));
        }

        /// <summary>
        /// 줌 인 / 줌 아웃 용 코루틴
        /// </summary>
        /// <param name="zoomsize"> 줌 사이즈</param>
        /// <param name="isZoomIn"> True : Zoom In  / False : Zoom Out </param>
        /// <returns></returns>
        private IEnumerator COR_ZoomInOut(float zoomsize, bool isZoomIn)
        {
            if (isZoomIn)
            {
                while (vCam.m_Lens.OrthographicSize >= zoomsize)
                {
                    vCam.m_Lens.OrthographicSize = Mathf.MoveTowards(vCam.m_Lens.OrthographicSize, zoomsize, Time.deltaTime);
                    yield return null;
                }
                vCam.m_Lens.OrthographicSize = zoomsize;
            }
            else
            {
                while (vCam.m_Lens.OrthographicSize <= zoomsize)
                {
                    vCam.m_Lens.OrthographicSize = Mathf.MoveTowards(vCam.m_Lens.OrthographicSize, zoomsize, Time.deltaTime);
                    yield return null;
                }
                vCam.m_Lens.OrthographicSize = zoomsize;
            }
        }

        #endregion Zoom Systems
    }
}