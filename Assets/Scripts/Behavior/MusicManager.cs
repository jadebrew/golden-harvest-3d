using UnityEngine;
using World;

namespace Behavior
{
    public class MusicManager : MonoBehaviour
    {
        public AudioClip[] songsPerHour;

        private AudioSource _bgm;
        private int _currentHour = -1;

        public void Start()
        {
            _bgm = GetComponent<AudioSource>();
            //_bgm.loop = false;
        }

        public void Update()
        {
            var cycle = DayNightCycle.Instance;
            if (!cycle || cycle.IsScrubbing)
            {
                return;
            }

            var hour = (int)cycle.CurrentHour;
            if (hour == _currentHour) return;

            _currentHour = hour;
            PlaySong(songsPerHour[hour]);
        }

        private void PlaySong(AudioClip song)
        {
            var previous = _bgm.clip;
            _bgm.Stop();
            if (previous && previous != song)
            {
                previous.UnloadAudioData();
            }

            _bgm.clip = song;
            _bgm.Play(); // Loads the clip's audio data on demand.
        }

        private void OnDestroy()
        {
            if (_bgm && _bgm.clip)
            {
                _bgm.clip.UnloadAudioData();
            }
        }
    }
}
