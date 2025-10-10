using UnityEngine;

public class ShrineController : MonoBehaviour
{
    public Animator animator;

    public void PlayGlowEffect()
    {
        if (animator == null) return;
          animator.SetTrigger("Play");
    }
}
