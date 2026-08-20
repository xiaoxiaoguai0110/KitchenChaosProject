using UnityEngine;

public static class FeedbackParticleFactory
{
    public static void PlayBurst(Vector3 position, Color color, int particleCount, float size = 0.13f)
    {
        GameObject effectObject = new GameObject("Feedback Burst");
        effectObject.transform.position = position;
        effectObject.SetActive(false);

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        // ParticleSystem 添加到激活对象时会立即按 Play On Awake 播放；
        // 配置 duration 前先保持对象禁用并彻底停止，避免 Unity 抛出运行时错误。
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.duration = 0.25f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.48f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.65f, size * 1.35f);
        main.startColor = color;
        main.gravityModifier = 0.45f;
        main.maxParticles = Mathf.Max(24, particleCount);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)particleCount)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.16f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fadeGradient = new Gradient();
        fadeGradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = fadeGradient;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Particles/Standard Unlit")
            ?? Shader.Find("Sprites/Default");
        if (particleShader != null)
        {
            Material material = new Material(particleShader);
            particleRenderer.material = material;
            Object.Destroy(material, 1.5f);
        }

        effectObject.SetActive(true);
        particles.Play();
        Object.Destroy(effectObject, 1.5f);
    }
}
