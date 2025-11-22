using UnityEngine;

public class CharacterAnimation : MonoBehaviour
{
    [SerializeField] Animator anim;
    [SerializeField, Range(-1, 1)] float walkMotion = 0;
    [SerializeField, Range(-1, 1)] float walkStrafe = 0;
    [SerializeField, Range(-90, 90)] float aimAngle = 0;

    [Space(12)]
    [SerializeField] bool inAir = false;

    public void SetValues(float walk, float strafe, float aim, bool grounded) {
        walkMotion = walk;
        walkStrafe = strafe;
        aimAngle = aim;
        inAir = grounded == false;
    }

    void Update() {

        // Local Forward and Backward Motion (1 = forward, -1 = back)
        anim.SetFloat("motion", walkMotion);

        // Local Right and Left Motion (1 = right, -1 = left)
        anim.SetFloat("strafe", walkStrafe);

        // Up and Down Aim Angle (90 = up, -90 = down)
        anim.SetFloat("aimAngle", aimAngle);

        // if the player is in the air or not!
        anim.SetBool("inAir", inAir);
    }
}
