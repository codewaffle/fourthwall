using FourthWall;
using UnityEngine;
using System.Collections;

namespace FourthWall { 
    public class FourthWallController : MonoBehaviour
    {
        public LayerMask Layers;
        private RaycastHit _hitInfo;
        public ExternalWindow CurrentExternalWindow;

        private ExternalWindow _dragging;

        void FixedUpdate()
        {
        }

        void Update()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out _hitInfo, 1024f, Layers))
            {
                CurrentExternalWindow = _hitInfo.transform.GetComponent<ExternalWindow>();
                var pt = _hitInfo.transform.InverseTransformPoint(_hitInfo.point);
                pt = (pt + (CurrentExternalWindow.Collider.size / 2f));
                pt.x /= CurrentExternalWindow.Collider.size.x;
                pt.y /= CurrentExternalWindow.Collider.size.y;
                pt.z = 0;

                CurrentExternalWindow.SetMouseCoord(pt);
            }
            else
            {
                CurrentExternalWindow = null;
            }

            if (CurrentExternalWindow != null)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _dragging = CurrentExternalWindow;
                    CurrentExternalWindow.SetMouseDown(0);
                }
            }

            if (Input.GetMouseButtonUp(0) && _dragging != null)
            {
                _dragging.SetMouseUp(0);
                _dragging = null;
            }
        }
    }
}