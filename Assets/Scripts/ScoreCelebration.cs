using UnityEngine;

public class ScoreCelebration : MonoBehaviour
{
    public ParticleSystem confetti;
    public AudioSource celebrationSound;

    void Start()
    {
        // Play confetti
        if (confetti != null)
        {
            confetti.Play();
        }

        // Play celebration sound
        if (celebrationSound != null)
        {
            celebrationSound.Play();
        }
    }
}