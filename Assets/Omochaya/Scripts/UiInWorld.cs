// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UiInWorld.cs" company="yoshikazu yananose">
//   (c) 2016 machi no omochaya-san.
// </copyright>
// <summary>
//   The ui in world.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Omochaya
{
    using UnityEngine;

    public class UiInWorld : MonoBehaviour
    {
        // fields (readonly)
        [SerializeField] private float size = 640f;
        [SerializeField] private RectTransform canvas = null;
        [SerializeField] private bool isScaling = true;

        // fields
        [SerializeField] private new Camera camera = null;

        // Start is called before the first frame update
        void Start()
        {
            if (this.camera == null)
            {
                this.camera = Camera.main;
            }

            this.canvas.pivot = Vector2.zero;
        }

        // Update is called once per frame
        void Update()
        {
            var camera = this.camera;
            var aspect = camera.rect.width * Screen.width / camera.rect.height / Screen.height;
            var fieldOfView = camera.fieldOfView;
            var view = Vector2.one * this.size;
            if (aspect < 1f)
            {
                view.y /= aspect;
            }
            else
            {
                view.x *= aspect;
            }

            this.canvas.anchoredPosition = -view / 2f;
            this.canvas.sizeDelta = view;

            var position = this.transform.position;
            var d = position - camera.transform.position;
            var distance = this.isScaling ? d.magnitude : 5f;
            var viewSize = distance * Mathf.Tan(fieldOfView * Mathf.PI / 360f) * 2;
            var scale = Vector3.one;
            scale.x = scale.y = viewSize / view.y;
            this.transform.localScale = scale;
            this.transform.LookAt(position + d, Vector3.up);
        }
    }
}