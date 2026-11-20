#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerGaspAudioTests
{
    [UnityTest]
    public IEnumerator EncounterGaspPlaysOnceAndSilencesBreathing()
    {
        var root = new GameObject("Gasp audio regression fixture");
        var clip = AudioClip.Create("Test gasp", 44100, 1, 44100, false);
        try
        {
            Type type = Type.GetType("PlayerAudioManager, Assembly-CSharp", true);
            var audio = root.AddComponent(type);
            AudioSource Source(string name)
            {
                var child = new GameObject(name);
                child.transform.SetParent(root.transform);
                var source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.clip = clip;
                return source;
            }
            var gasp = Source("Gasp");
            var sprint = Source("Sprint");
            var recover = Source("Recover");
            const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
            type.GetField("GaspAudioSource", fields).SetValue(audio, gasp);
            type.GetField("SprintingBreathingAudioSource", fields).SetValue(audio, sprint);
            type.GetField("RecoveringBreathingAudioSource", fields).SetValue(audio, recover);
            sprint.Play(); recover.Play();
            var setEncounter = type.GetMethod("SetMonsterEncounterActive");
            setEncounter.Invoke(audio, new object[] { true });
            Assert.That(gasp.isPlaying, Is.True, "Encounter must start the gasp");
            Assert.That(gasp.priority, Is.LessThan(128), "Gasp must take priority over ordinary ambience");
            Assert.That((float)type.GetField("GaspVolumeMultiplier", fields).GetValue(audio), Is.GreaterThan(1f));
            Assert.That(sprint.volume, Is.Zero);
            Assert.That(recover.volume, Is.Zero);
            Assert.That(sprint.isPlaying || recover.isPlaying, Is.False, "Breathing must not mask the short gasp");
            gasp.Stop();
            setEncounter.Invoke(audio, new object[] { true });
            yield return null;
            Assert.That(gasp.isPlaying, Is.False, "Repeated active updates must not restart the gasp");
            setEncounter.Invoke(audio, new object[] { false });
            setEncounter.Invoke(audio, new object[] { true });
            Assert.That(gasp.isPlaying, Is.True, "A new encounter must gasp again");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(clip);
        }
    }
}
#endif
