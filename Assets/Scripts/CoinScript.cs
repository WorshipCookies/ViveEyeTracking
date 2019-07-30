using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinScript : MonoBehaviour
{
    public float m_RotationSpeed = 30f;
    public AudioSource m_AudioSource;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(new Vector3(0f,m_RotationSpeed*Time.deltaTime,0f));
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player")
        {
            StartCoroutine(PlayAudioFile());
        }
    }

    IEnumerator PlayAudioFile()
    {
        m_AudioSource.Play();
        yield return new WaitForSeconds(m_AudioSource.clip.length);
        Destroy(this.gameObject);
    }
}
