using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class SmoothVideoLoop : MonoBehaviour
{
    public VideoPlayer videoPlayerA;
    public VideoPlayer videoPlayerB;
    public RawImage targetDisplay;
    public float overlapTime = 0.1f;
    private bool isAPlaying = true;
    private double videoLength;

    void Start()
    {
        PrepareVideoPlayer(videoPlayerA);
        PrepareVideoPlayer(videoPlayerB);
        targetDisplay.texture = videoPlayerA.targetTexture;
        videoPlayerA.Prepare();
        videoPlayerA.prepareCompleted += (vp) =>
        {
            videoLength = vp.length;
            vp.time = 0;
            vp.Play();
        };
    }

    void Update()
    {
        VideoPlayer current = isAPlaying ? videoPlayerA : videoPlayerB;
        VideoPlayer next = isAPlaying ? videoPlayerB : videoPlayerA;
        if (current.isPlaying && current.time >= videoLength - overlapTime && !next.isPlaying)
        {
            next.time = 0;
            next.Play();
            targetDisplay.texture = next.targetTexture;
            current.Stop();
            isAPlaying = !isAPlaying;
        }
    }

    void PrepareVideoPlayer(VideoPlayer vp)
    {
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.waitForFirstFrame = false;
        vp.skipOnDrop = true;
    }
}