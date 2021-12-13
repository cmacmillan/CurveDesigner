using UnityEngine;

namespace ChaseMacMillan.CurveDesigner.Examples
{
    public class EndlessRunnerObstacle : MonoBehaviour
    {
        public GameObject bloodParticlesPrefab;
        public AudioClip dealDamageSFX;
        private void OnTriggerEnter(Collider other)
        {
            var character = other.transform.parent.GetComponent<EndlessRunnerCharacter>();//collider is a child of the character script
            if (character != null)
            {
                PlaySound.Play(dealDamageSFX, other.transform.position,.5f);
                var particles = Instantiate(bloodParticlesPrefab);
                particles.transform.position = other.transform.position;
                particles.transform.rotation = transform.rotation;
                character.health--;
            }
        }
    }
}
