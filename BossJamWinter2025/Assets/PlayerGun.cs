using Fusion;
using UnityEngine;


public class PlayerGun : MonoBehaviour
{
    [SerializeField] BounceRay bulletPrefab;
    [SerializeField] Animator gunAnim;

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    public void RPC_ReportCosmeticBullet(Vector3 worldPosition, Quaternion rotation, Vector3 gunFirePoint, RpcInfo info = default) {
        BounceRay instance = Instantiate(bulletPrefab, worldPosition, rotation);
        PlayShootAnim();
        instance.Shoot(gunFirePoint, !info.IsInvokeLocal);
    }

    public void PlayShootAnim() {
        gunAnim.Play("Shoot");
    }
}
