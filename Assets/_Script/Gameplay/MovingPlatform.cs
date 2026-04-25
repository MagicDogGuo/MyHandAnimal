using UnityEngine;

/// <summary>第四關等：左右正弦往復移動；速度可由 <see cref="LevelConfig.platformSpeed"/> 帶入。</summary>
public class MovingPlatform : MonoBehaviour
{
    public float speed  = 1.5f;
    public float range  = 2.0f;
    [SerializeField] bool axisX = true;

    Vector3 _origin;

    void Start()
    {
        _origin = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * range;
        if (axisX)
            transform.position = _origin + Vector3.right * offset;
        else
            transform.position = _origin + Vector3.forward * offset;
    }

    public void ApplyFromConfig(LevelConfig config)
    {
        if (config == null) return;
        speed = config.platformSpeed;
    }
}
