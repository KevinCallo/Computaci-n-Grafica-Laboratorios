using UnityEngine;
using System.Collections.Generic;

public class MovingPlatform2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector2 moveDirection = Vector2.right;
    public float distance = 5f;
    public float speed = 2f;

    private Vector3 startPos;
    private Vector3 lastPosition;
    private List<Transform> passengers = new List<Transform>();

    void Start()
    {
        startPos = transform.position;
        lastPosition = transform.position;
    }

    // Usamos LateUpdate para que la plataforma se mueva DESPUÉS que el jugador
    void LateUpdate()
    {
        // 1. Calcular nueva posición
        float movement = Mathf.PingPong(Time.time * speed, distance);
        Vector3 targetPosition = startPos + (Vector3)(moveDirection.normalized * movement);

        // 2. Calcular cuánto nos hemos movido en este frame (el Delta)
        Vector3 velocity = targetPosition - transform.position;

        // 3. Mover a todos los pasajeros (jugador, enemigos, etc.) esa misma distancia
        foreach (Transform passenger in passengers)
        {
            if (passenger != null)
            {
                passenger.position += velocity;
            }
        }

        // 4. Mover la plataforma
        transform.position = targetPosition;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Si lo que nos toca es el jugador y está encima de nosotros (no a los lados)
        if (collision.gameObject.CompareTag("Player"))
        {
            // Solo lo movemos si cae desde arriba
            if (collision.contactCount > 0 && collision.contacts[0].normal.y < -0.5f)
            {
                if (!passengers.Contains(collision.transform))
                {
                    passengers.Add(collision.transform);
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (passengers.Contains(collision.transform))
            {
                passengers.Remove(collision.transform);
            }
        }
    }

    // Seguridad: si la plataforma se borra o apaga, limpiamos la lista
    private void OnDisable()
    {
        passengers.Clear();
    }
}
