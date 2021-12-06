using UnityEngine;

namespace ChaseMacMillan.CurveDesigner.Examples
{
    public class EndlessRunnerCoin : MonoBehaviour
    {
        public Transform child;
        public float rotationSpeed;
        public GameObject getCoinParticlePrefab;
        public AudioClip collectionSound;
        void Update()
        {
            child.transform.rotation = Quaternion.AngleAxis(rotationSpeed * Time.deltaTime, transform.up) * child.transform.rotation;
        }
        private void OnTriggerEnter(Collider other)
        {
            var character = other.transform.parent.GetComponent<EndlessRunnerCharacter>();//collider is a child of the character script
            if (character != null)
            {
                PlaySound.Play(collectionSound, 0.3f, transform.position);
                var particles = Instantiate(getCoinParticlePrefab);
                particles.transform.position = child.transform.position;
                particles.transform.rotation = child.transform.rotation;
                character.coinCount++;
                Destroy(gameObject);
            }
        }
    }
}
