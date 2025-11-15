using UnityEngine;

/// <summary>
/// Sistema mejorado de bala usando Raycast.
/// Más preciso que colisiones físicas.
/// Incluye efectos de impacto en paredes y enemigos.
/// Daña tanto a enemigos como al jugador.
/// </summary>
public class BulletScript : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damage = 25f;
    public float hitForce = 100f;

    [Header("Bullet Settings")]
    public float bulletSpeed = 50f;
    public float lifetime = 5f;
    public bool useRaycast = true;
    public float maxRayDistance = 2f;

    [Header("Effects")]
    public GameObject impactEffect;
    public GameObject bloodEffect;
    public GameObject decalHitWall;
    public float floatInfrontOfWall = 0.05f;

    private Vector3 lastPosition;
    private bool hasHit = false;
    private LayerMask hitMask;

    void Start()
    {
        lastPosition = transform.position;

        int weaponLayer = LayerMask.NameToLayer("Weapon");
        hitMask = ~(1 << weaponLayer); // Solo excluir la capa Weapon

        Destroy(gameObject, lifetime);

        Debug.Log("[BulletScript] Bala creada. Speed: " + bulletSpeed);
    }

    void Update()
    {
        if (hasHit) return;

        Vector3 movement = transform.forward * bulletSpeed * Time.deltaTime;

        if (useRaycast)
        {
            RaycastHit hit;
            float distance = movement.magnitude;

            if (Physics.Raycast(lastPosition, transform.forward, out hit, distance + maxRayDistance, hitMask))
            {
                Debug.Log("[BulletScript] Impacto con: " + hit.collider.name + " Tag: " + hit.collider.tag);
                ProcessHit(hit.collider.gameObject, hit.point, hit.normal);
                return;
            }
        }

        transform.position += movement;
        lastPosition = transform.position;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        Vector3 hitPoint = collision.contacts[0].point;
        Vector3 hitNormal = collision.contacts[0].normal;

        ProcessHit(collision.gameObject, hitPoint, hitNormal);
    }

    void ProcessHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        hasHit = true;

        // Buscar EnemyHealth de forma inteligente
        EnemyHealth enemyHealth = hitObject.GetComponent<EnemyHealth>()
            ?? hitObject.transform.parent?.GetComponent<EnemyHealth>()
            ?? hitObject.transform.root?.GetComponent<EnemyHealth>()
            ?? hitObject.GetComponentInChildren<EnemyHealth>();

        // Buscar PlayerHealth
        PlayerHealth playerHealth = hitObject.GetComponent<PlayerHealth>()
            ?? hitObject.transform.parent?.GetComponent<PlayerHealth>()
            ?? hitObject.transform.root?.GetComponent<PlayerHealth>()
            ?? hitObject.GetComponentInChildren<PlayerHealth>();

        // <-- NUEVO: Buscar Objeto Explosivo
        ExplosiveObject explosiveObject = hitObject.GetComponent<ExplosiveObject>()
            ?? hitObject.transform.parent?.GetComponent<ExplosiveObject>()
            ?? hitObject.transform.root?.GetComponent<ExplosiveObject>()
            ?? hitObject.GetComponentInChildren<ExplosiveObject>();

        // Verificar si es enemigo por tag
        bool isEnemyByTag = hitObject.CompareTag("Dummie");

        // Verificar si es jugador por tag
        bool isPlayerByTag = hitObject.CompareTag("Player");

        // <-- NUEVO: Añadido chequeo de explosiveObject al log
        Debug.Log("[BulletScript] Objeto golpeado: " + hitObject.name + " | EnemyHealth: " + (enemyHealth != null) + " | PlayerHealth: " + (playerHealth != null) + " | Explosive: " + (explosiveObject != null) + " | PlayerTag: " + isPlayerByTag);

        // DAÑO AL JUGADOR - Debe ser lo primero
        if (playerHealth != null || isPlayerByTag)
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log("[BulletScript] ✓ DAÑO AL JUGADOR: " + damage);
            }

            // Efecto de sangre al impactar jugador
            if (bloodEffect)
                Destroy(Instantiate(bloodEffect, hitPoint, Quaternion.LookRotation(hitNormal)), 2f);
        }
        // DAÑO A ENEMIGOS - Si no es el jugador
        else if (enemyHealth != null || isEnemyByTag)
        {
            if (enemyHealth != null)
                enemyHealth.TakeDamage(damage, hitPoint, transform.forward);

            // Efecto de sangre al impactar enemigo
            if (bloodEffect)
                Destroy(Instantiate(bloodEffect, hitPoint, Quaternion.LookRotation(hitNormal)), 2f);

            Debug.Log("[BulletScript] ✓ DAÑO A ENEMIGO: " + damage);
            
            GunScript.HitMarkerSound();
        }
        // <-- NUEVO: DAÑO A OBJETO EXPLOSIVO
        else if (explosiveObject != null)
        {
            explosiveObject.TakeDamage(damage);
            
            Debug.Log("[BulletScript] ✓ DAÑO A EXPLOSIVO: " + damage);

            // Efecto de chispas (puedes usar el de impacto genérico)
             if (impactEffect)
                Destroy(Instantiate(impactEffect, hitPoint + hitNormal * 0.01f, Quaternion.LookRotation(hitNormal)), 3f);
            
            // Opcional: puedes decidir si quieres sonido de hitmarker al barril
            // GunScript.HitMarkerSound(); 
        }
        // IMPACTO EN PAREDES - Si no es jugador ni enemigo
        else
        {
            // Efecto impacto pared
            if (impactEffect)
                Destroy(Instantiate(impactEffect, hitPoint + hitNormal * 0.01f, Quaternion.LookRotation(hitNormal)), 3f);

            // Agujero de bala (decal) solo en pared con tag "LevelPart"
            if (decalHitWall && hitObject.CompareTag("LevelPart"))
                Destroy(Instantiate(decalHitWall, hitPoint + hitNormal * floatInfrontOfWall, Quaternion.LookRotation(hitNormal)), 10f);
        }

        // Fuerza física si objeto tiene rigidbody
        Rigidbody rb = hitObject.GetComponent<Rigidbody>();
        if (rb && !rb.isKinematic)
            rb.AddForceAtPosition(transform.forward * hitForce, hitPoint, ForceMode.Impulse);

        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.5f);
    }
}