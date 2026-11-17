using UnityEngine;
using World;

namespace Behavior
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
            if (!cycle || cycle.IsScrubbing)
            {
                return;
            }

            SetNewSong(cycle);
        }

        private void SetNewSong(DayNightCycle cycle)
        {
            var currentSong = songsPerHour[(int)cycle.CurrentHour];
            if (_bgm.clip.ToString() == currentSong.ToString()) return;

            _bgm.clip = currentSong;
            _bgm.Play();
        }
    }
}
