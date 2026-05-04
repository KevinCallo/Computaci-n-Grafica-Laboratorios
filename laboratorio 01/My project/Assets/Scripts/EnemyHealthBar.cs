using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    public Slider slider;
    public Image fillImage;
    private Transform target;
    private float yOffset = 2.5f;
    private Camera cam;

    void Start() { cam = Camera.main; }

    public void Setup(Transform enemyTransform, float maxHealth)
    {
        target = enemyTransform;
        if (slider) { slider.maxValue = maxHealth; slider.value = maxHealth; }
    }

    public void UpdateHealth(float hp)
    {
        if (slider) slider.value = Mathf.Max(0, hp);
        if (fillImage)
        {
            float ratio = slider ? (slider.value / slider.maxValue) : 1f;
            fillImage.color = ratio > 0.6f ? Color.green : ratio > 0.3f ? new Color(1f, 0.5f, 0f) : Color.red;
        }
    }

    void LateUpdate()
    {
        if (target == null) { Destroy(gameObject); return; }
        transform.position = target.position + Vector3.up * yOffset;
        if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
