using Unity.Netcode;
using UnityEngine;

public class EnemyController : BaseCharacter 
{
    // ... (AI 로직, 타겟 설정 등) ...

    protected override void OnDeath()
    {
        // 서버에서만 Despawn을 실행할 수 있습니다.
        if (IsServer)
        {
            NetworkObject.Despawn(false);
        }
    }
}