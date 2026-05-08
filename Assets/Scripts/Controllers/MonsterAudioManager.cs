using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAudioManager : MonoBehaviour
{
    [SerializeField] GameObject FootStepSourceObject;
    List<AudioSource> _footstepAudioSources;
    Coroutine _footstepCoroutine;

    const float _footstepSoundDelay = 0.5f;
    const int _numberOfFootsteps = 3;

    void Awake()
    {
        _footstepAudioSources = new List<AudioSource>();
        if (FootStepSourceObject != null)
        {
            _footstepAudioSources.AddRange(FootStepSourceObject.GetComponentsInChildren<AudioSource>());
        }
    }

    public void PlayFootstepsSound()
    {
        if (_footstepCoroutine != null)
        {
            StopCoroutine(_footstepCoroutine);
        }

        _footstepCoroutine = StartCoroutine(PlayFootstepsWithDelay(_footstepSoundDelay));
    }

    private void PlayRandomFootstepSound()
    {
        if (_footstepAudioSources.Count == 0)
        {
            return;
        }

        int index = Random.Range(0, _footstepAudioSources.Count);
        AudioSource footstepSource = _footstepAudioSources[index];
        if (footstepSource.clip != null)
        {
            footstepSource.PlayOneShot(footstepSource.clip);
        }
    }

    IEnumerator PlayFootstepsWithDelay(float delay)
    {
        for (int i = 0; i < _numberOfFootsteps; i++)
        {
            PlayRandomFootstepSound();
            yield return new WaitForSeconds(delay);
        }

        _footstepCoroutine = null;
    }
}
