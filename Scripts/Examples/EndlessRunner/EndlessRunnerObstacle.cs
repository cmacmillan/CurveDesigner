using UnityEngine;

namespace ChaseMacMillan.CurveDesigner.Examples
{
    public class EndlessRunnerObstacle : MonoBehaviour
    {
        public GameObject bloodParticlesPrefab;
        private void OnTriggerEnter(Collider other)
        {
            var character = other.transform.parent.GetComponent<EndlessRunnerCharacter>();//collider is a child of the character script
            if (character != null)
            {
                var particles = Instantiate(bloodParticlesPrefab);
                particles.transform.position = other.transform.position;
                particles.transform.rotation = transform.rotation;
                character.health--;
            }
        }
    }
}
