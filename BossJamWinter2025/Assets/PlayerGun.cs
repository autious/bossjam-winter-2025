using Fusion;
using UnityEngine;


public class PlayerGun : MonoBehaviour
{
    [SerializeField] BounceRay bulletPrefab;
    [SerializeField] Animator gunAnim;
    

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    public void RPC_ReportCosmeticBullet(Vector3 worldPosition, Quaternion rotation, RpcInfo info = default) {
        BounceRay instance = Instantiate(bulletPrefab, worldPosition, rotation);
        instance.isCosmetic = !info.IsInvokeLocal;
    }

    public void PlayShootAnim() {
        gunAnim.Play("Shoot");
    }
}
