using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spherecollision : MonoBehaviour
{
    // Start is called before the first frame update
    public AudioSource audio;
    public AudioClip[] bounceSounds;
    public float velocityMin=.5f;
    public float velocityMax=10;
    private float lifetime=0;
    private Rigidbody rigid;

    private void Start()
    {
        rigid = this.GetComponent<Rigidbody>();
    }
    private void Update()
    {
        lifetime += Time.deltaTime; 
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.other.name!="Curve")
        {
            float velocity = collision.relativeVelocity.magnitude;
            if (velocity > velocityMin)
            {
                float volume = Mathf.Clamp01((velocity - velocityMin) / (velocityMax - velocityMin));
                audio.PlayOneShot(bounceSounds[Random.Range(0, bounceSounds.Length)], volume);
           }
            rigid.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
    }
}
