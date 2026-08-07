using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PBF(Position Based Fluids) 파티클 하나의 상태.
///
/// 힘으로 밀도를 맞추던 방식과 달리, 매 스텝 "예측 위치"를 잡아두고 밀도 제약을 풀어 그 위치를 직접
/// 보정한다. 힘은 중력에 밀릴 수 있지만(그래서 액체가 바닥에 짓눌렸다) 위치 제약은 반복해서 풀면
/// 반드시 지켜지므로, 중력이 아무리 세도 액체가 제 부피를 유지한다.
///
/// 벽 충돌은 Unity 물리를 쓰지 않고 PourManager가 컨테이너 로컬 좌표로 직접 클램프한다.
/// </summary>
public class SphParticle : MonoBehaviour
{
    /// <summary>확정된 현재 위치. 한 스텝이 끝날 때 predictedPos로 갱신된다.</summary>
    public Vector2 pos;
    public Vector2 velocity;

    /// <summary>이번 스텝에 도달할 것으로 예상되는 위치. 솔버가 이 값을 반복해서 보정한다.</summary>
    public Vector2 predictedPos;

    /// <summary>이번 반복에서 누적한 위치 보정량.</summary>
    public Vector2 deltaPos;

    /// <summary>밀도 제약의 라그랑주 승수. 밀도가 기준보다 높을수록 음수가 되어 파티클을 밀어낸다.</summary>
    public float lambda;

    public float density;

    public readonly List<SphParticle> neighbours = new List<SphParticle>();

    public int gridX;
    public int gridY;

    /// <summary>병의 열린 입구를 한 번이라도 넘었으면 true. 병은 회전하므로 매번 로컬 좌표로 재판정하면
    /// 이미 빠져나간 파티클도 벽에 부딪힌 것으로 오판해 붙잡히는데, 이 표식으로 그걸 막는다.</summary>
    public bool exitedBottle;

    /// <summary>이웃 속도 평균으로 부드럽게 만든 값을 잠시 담아두는 버퍼(XSPH 점성).</summary>
    public Vector2 smoothedVelocity;

    SphConfig config;

    public void Init(SphConfig sphConfig)
    {
        config = sphConfig;
    }

    /// <summary>파티클을 spawnPos에서 정지 상태로 활성화한다(풀 재사용 포함).</summary>
    public void Activate(Vector2 spawnPos)
    {
        pos = spawnPos;
        predictedPos = spawnPos;
        velocity = Vector2.zero;
        deltaPos = Vector2.zero;
        lambda = 0f;
        density = 0f;
        exitedBottle = false;

        transform.position = spawnPos;
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    /// <summary>중력을 적용하고 이번 스텝의 예측 위치를 잡는다(PBF 1단계).</summary>
    public void PredictPosition(float dt)
    {
        velocity += new Vector2(0f, -config.gravity) * dt;

        if (velocity.magnitude > config.maxVelocity)
            velocity = velocity.normalized * config.maxVelocity;

        predictedPos = pos + velocity * dt;
    }

    /// <summary>보정이 끝난 예측 위치를 확정하고, 실제로 움직인 거리에서 속도를 되뽑는다(PBF 마지막 단계).</summary>
    public void ApplyPrediction(float dt)
    {
        velocity = (predictedPos - pos) / Mathf.Max(dt, 0.0001f);
        pos = predictedPos;

        // Transform은 일부러 갱신하지 않는다. 파티클 오브젝트에는 렌더러가 없고 위치를 읽는 곳도 없어서
        // (렌더러와 잔 판정 모두 pos를 쓴다) 여기서 transform.position에 쓰면 파티클 수 x 서브스텝만큼
        // 네이티브 호출이 나가는 게 전부다. 대신 Hierarchy에서는 파티클이 스폰 위치에 멈춰 보인다.
    }
}
