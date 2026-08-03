using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SPH 파티클 하나의 물리 상태. AlexandreSajus/Unity-Fluid-Simulation의 Particle.cs를 이식했다.
///
/// Rigidbody2D는 Dynamic + gravityScale 0으로 둔다. 실제 이동은 이 스크립트가 힘(중력+압력+점성)을
/// 직접 오일러 적분해 transform.position에 쓰는데, 그러려면 벽(Static Collider2D)과의
/// OnCollisionStay2D 콜백이 필요하다 — Kinematic으로 두면 Static 콜라이더와는 충돌 이벤트 자체가
/// 발생하지 않기 때문에, 원본 레포처럼 Dynamic + gravityScale 0 조합을 그대로 따른다
/// (물리엔진 자체의 낙하/이동은 안 쓰고 충돌 감지 용도로만 사용).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
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
    Rigidbody2D body;
    CircleCollider2D circleCollider;

    public void Init(SphConfig sphConfig, float radius)
    {
        config = sphConfig;

        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.mass = 1f;
        // 벽이 얇고 파티클이 압력힘으로 순간적으로 빠르게 밀릴 수 있어, Discrete로는 한 프레임에
        // 벽을 그냥 통과(터널링)해버리는 경우가 있었다. Continuous로 바꿔 얇은 벽도 확실히 막는다.
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.radius = radius;
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

    public void CalculatePressure()
    {
        pressure = config.pressureStiffness * (density - config.restDensity);
        pressureNear = config.nearPressureStiffness * densityNear;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        ContactPoint2D contact = collision.GetContact(0);
        Vector2 normal = contact.normal;

        float velocityAlongNormal = Vector2.Dot(velocity, normal);
        if (velocityAlongNormal > 0f) return;

        Vector2 tangentVelocity = velocity - normal * velocityAlongNormal;
        velocity = tangentVelocity - normal * velocityAlongNormal * config.wallDamp;

        pos = contact.point + normal * circleCollider.radius;
        transform.position = pos;
    }
}
