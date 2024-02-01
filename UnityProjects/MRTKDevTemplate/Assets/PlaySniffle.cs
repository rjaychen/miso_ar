using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySniffle : MonoBehaviour
{
    AudioSource aud;
    [SerializeField] private Animator myAnimator;
    // Start is called before the first frame update
    void Start()
    {
        aud = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void play_sound()
    {
        GetComponent<AudioSource>().Play();
    }

    public void StartAnimation()
    {
        myAnimator.SetBool("playSniffle", true);
    }

}
