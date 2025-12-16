using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace RuntimeModelLoaders.Interaction
{
    /// <summary>
    /// XR Grab Interactable, das Zwei-Hand-Skalierung ermöglicht und beim Loslassen
    /// sanft das Rigidbody dämpft. Rotation/Translation übernimmt das Toolkit automatisch.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ModelPlacementController : XRGrabInteractable
    {
        [Tooltip("Multiplikator für die Zwei-Hand-Skalierung.")]
        public float scaleSensitivity = 1.0f;

        [Tooltip("Grenzen für Skalierung, um Extremwerte zu vermeiden.")]
        public Vector2 scaleClamp = new Vector2(0.05f, 10f);

        private float _initialDistance;
        private Vector3 _initialScale;

        protected override void Awake()
        {
            base.Awake();
            // Rigidbody-Einstellungen für stabiles Greifen im VR
            var rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            CacheTwoHandDataIfNeeded();
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            CacheTwoHandDataIfNeeded();
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                return;

            if (interactorsSelecting.Count >= 2)
            {
                var a = interactorsSelecting[0].transform.position;
                var b = interactorsSelecting[1].transform.position;
                var currentDistance = Vector3.Distance(a, b);

                if (_initialDistance > 0.001f)
                {
                    var scaleFactor = (currentDistance / _initialDistance) * scaleSensitivity;
                    var targetScale = _initialScale * scaleFactor;
                    var clamped = Mathf.Clamp(targetScale.x, scaleClamp.x, scaleClamp.y);
                    transform.localScale = Vector3.one * clamped;
                }
            }
        }

        private void CacheTwoHandDataIfNeeded()
        {
            if (interactorsSelecting.Count >= 2)
            {
                var a = interactorsSelecting[0].transform.position;
                var b = interactorsSelecting[1].transform.position;
                _initialDistance = Vector3.Distance(a, b);
                _initialScale = transform.localScale;
            }
            else
            {
                _initialDistance = 0f;
            }
        }
    }
}
