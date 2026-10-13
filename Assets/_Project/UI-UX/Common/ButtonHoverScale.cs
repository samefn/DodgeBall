using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.07f;
    [SerializeField] private float pressedScale = 0.96f;
    [SerializeField] private float speed = 15f;

    private Vector3 baseScale = Vector3.one;
    private float targetScale = 1f;
    private bool hovered;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        hovered = false;
        targetScale = 1f;
        transform.localScale = baseScale;
    }

    private void Update()
    {
        float blend = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * targetScale, blend);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        targetScale = hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        targetScale = 1f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = hovered ? hoverScale : 1f;
    }
}
