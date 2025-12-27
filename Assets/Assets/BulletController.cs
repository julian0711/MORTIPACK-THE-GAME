using UnityEngine;

public class BulletController : MonoBehaviour
{
    private Vector2 direction;
    private float speed = 30f; // Fast speed as requested (3x)
    private float lifetime = 2f; // Auto destroy if nothing hits

    public void Initialize(Vector2 dir)
    {
        direction = dir.normalized;
        
        // Rotate bullet to match direction (optional visual polish)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        lifetime -= Time.deltaTime;
        if (lifetime <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            Destroy(gameObject); // Hit wall, disappear
        }
        else
        {
            EnemyMovement enemy = other.GetComponent<EnemyMovement>();
            if (enemy != null)
            {
                enemy.WarpToRandomPosition();
                Destroy(gameObject); // Hit enemy, warp it and disappear
            }
        }
    }
}
