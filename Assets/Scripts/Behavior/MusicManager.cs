using UnityEngine;

namespace World
{
    public class MusicManager : MonoBehaviour
    {
        public AudioClip[] songsPerHour;

        private AudioSource _bgm;

        public void Start()
        {
            _bgm = GetComponent<AudioSource>();
        }

        public void Update()
        {
            var cycle = DayNightCycle.Instance;
            if (!cycle)
            {
                return;
            }

            var currentSong = songsPerHour[(int)cycle.CurrentHour];
            if (_bgm.clip.ToString() != currentSong.ToString())
            {
                _bgm.clip = currentSong;
                _bgm.Play();
            }
        }
    }
}
