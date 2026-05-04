using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PushableObject2D : TransformableObject
{
    [Header("Pushable Settings")]
    public float mass = 1f;
    public float linearDrag = 0.5f;

    protected override void Awake()
    {
        // Esto permite que el objeto siga siendo escalable/rotatable si quieres
        base.Awake();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        
        // Configuramos el Rigidbody para que sea físico
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = mass;
        rb.linearDamping = linearDrag; // Evita que se deslice como si fuera hielo
        
        // Evitamos que el objeto ruede como una pelota
        rb.freezeRotation = true; 
        
        // Detección de colisión continua para que no atraviese el suelo
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    // Si quieres que al empujarlo pase algo especial, puedes añadirlo aquí
    protected override void OnTransformed(string type)
    {
        base.OnTransformed(type);
    }
    protected override void Update()
{
    // Llamamos a la base para que la escala y rotación sigan funcionando
    base.Update();

    // TRUCO: Actualizamos la posición objetivo para que el script base
    // no intente "tirar" del objeto hacia atrás mientras lo empujas.
    targetPosition = transform.position;
}
}