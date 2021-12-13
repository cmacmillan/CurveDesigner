using UnityEngine;

namespace ChaseMacMillan.CurveDesigner.Examples
{
    public class PlaySound : MonoBehaviour
    {
        private AudioSource source;
        void Update()
        {
            if (source == null || !source.isPlaying)
                Destroy(gameObject);
        }
        public static void Play(AudioClip clip, Vector3 position,float volume=1,float pitch=1)
        {
            var audio = new GameObject("AudioSource");
            audio.transform.position = position;
            var playSound = audio.AddComponent<PlaySound>();
            var source = audio.AddComponent<AudioSource>();
            playSound.source = source;
            source.pitch = pitch;
            source.volume = volume;
            source.clip = clip;
            source.volume = volume;
            source.loop = false;
            source.Play();
        }
    }
}
