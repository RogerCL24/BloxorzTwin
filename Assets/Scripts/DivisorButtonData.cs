using UnityEngine;

/// <summary>
/// Almacena los datos de configuración para un botón divisor.
/// Se adjunta al GameObject del divisor button y contiene las posiciones
/// donde los cubos divididos deberían aparecer.
/// </summary>
public class DivisorButtonData : MonoBehaviour
{
    public Vector3 splitPositionA;
    public Vector3 splitPositionB;
}
