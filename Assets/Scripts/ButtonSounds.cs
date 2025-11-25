using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Necesario para detectar el ratón

[RequireComponent(typeof(Button))]
public class ButtonSounds : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public AudioSource audioSource; // Arrastra aquí tu 'AmbiencePlayer' o crea uno para SFX
    public AudioClip hoverSound;    // Sonido al pasar el ratón
    public AudioClip clickSound;    // Sonido al hacer clic

    // Se activa cuando el ratón entra en el botón
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    // Se activa cuando haces clic
    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}