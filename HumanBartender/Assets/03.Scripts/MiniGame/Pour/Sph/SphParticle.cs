using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SPH 파티클 하나의 물리 상태. AlexandreSajus/Unity-Fluid-Simulation의 Particle.cs를 이식했다.
///
/// 벽 충돌은 Unity 물리(Rigidbody2D/Collider2D)를 쓰지 않고 PourManager.ConstrainParticles()가
/// 컨테이너 로컬 좌표로 직접 클램프한다. 둘을 같이 쓰면 콜라이더가 미는 위치와 클램프가 미는 위치가
/// 미묘하게 달라 파티클이 매 프레임 두 지점을 오가며 떨렸다. 물리 컴포넌트를 안 붙이는 쪽이
/// 그 진동이 없고, 파티클마다 강체를 만들지 않아 훨씬 가볍다.
/// </summary>
public class SphParticle : MonoBehaviour
{
    public Vector2 pos;
    public Vector2 previousPos;
    public float density;
    public float densityNear;
    public float pressure;
    public float pressureNear;
    public Vector2 velocity;
    public Vector2 force;

    public readonly List<SphParticle> neighbours = new List<SphParticle>();

    public int gridX;
    public int gridY;

    /// <summary>병의 열린 입구(로컬 +Y)를 한 번이라도 넘었으면 true. PourManager가 이후로는 이 파티클을
    /// 다시는 병의 로컬 좌표계로 재해석하지 않게(=회전 중인 좌표계에서 잘못 갇히지 않게) 쓴다.</summary>
    public bool exitedBottle;

    SphConfig config;

    /// <summary>이웃 속도 평균으로 부드럽게 만든 값을 잠시 담아두는 버퍼. 순서에 따라 결과가 달라지지
    /// 않도록 전부 계산한 뒤 한꺼번에 반영한다.</summary>
    public Vector2 smoothedVelocity;

    public void Init(SphConfig sphConfig)
    {
        config = sphConfig;
    }

    /// <summary>파티클을 spawnPos에서 정지 상태로 다시 활성화한다(재시도 시 풀 재사용용).</summary>
    public void Activate(Vector2 spawnPos)
    {
        pos = spawnPos;
        previousPos = spawnPos;
        velocity = Vector2.zero;
        force = Vector2.zero;
        exitedBottle = false;
        transform.position = spawnPos;

        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void ResetForFrame()
    {
        density = 0f;
        densityNear = 0f;
        neighbours.Clear();
        force = new Vector2(0f, -config.gravity);
    }

    public void Integrate(float dt)
    {
        previousPos = pos;

        velocity += force * dt;
        pos += velocity * dt;

        transform.position = pos;

        velocity = (pos - previousPos) / Mathf.Max(dt, 0.0001f);

        if (velocity.magnitude > config.maxVelocity)
            velocity = velocity.normalized * config.maxVelocity;
    }

    /// <summary>restDensity는 SphSimulation이 스폰 격자에서 실측해 넘겨준다(SphConfig의 값은 보정 전 초기값).</summary>
    public void CalculatePressure(float restDensity)
    {
        pressure = config.pressureStiffness * (density - restDensity);
        pressureNear = config.nearPressureStiffness * densityNear;
    }

}
