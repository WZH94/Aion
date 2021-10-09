using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction
{
    [Serializable]
    public class OrbitingUI
    {
        [SerializeField] private GameObject uiCanvas;
        [SerializeField] private float frontOffset;
        [SerializeField] private float upOffset;
        [SerializeField] private bool negativeUpOffset;
        [SerializeField] private Vector3 setAngle;

        private Vector3 _horizontalVectorComponent;
        private Vector3 _verticalVectorComponent;

        private bool _inTransition;

        public void CalcComponents()
        {
            //_horizontalVectorComponent = Vector3.forward * frontOffset;
        }

        public void OrbitOrCheck(Transform headTracker, Vector3 _startPosition, float rotationTreshold)
        {
            if (uiCanvas.activeSelf)
            {
                if (_inTransition)
                {
                    if ((int) uiCanvas.transform.eulerAngles.y == (int) headTracker.eulerAngles.y)
                    {
                        _inTransition = false;
                    }
                    else
                    {
                        uiCanvas.transform.rotation = Quaternion.Lerp(uiCanvas.transform.rotation,
                            Quaternion.Euler(new Vector3(0, headTracker.eulerAngles.y, 0) + setAngle), .03f);
                        uiCanvas.transform.position =
                            (_startPosition +
                             (Quaternion.Euler(0, uiCanvas.transform.eulerAngles.y, 0) * Vector3.forward * frontOffset) +
                             (Vector3.up * upOffset));
                    }
                }
                else
                {
                    if (Mathf.Abs(uiCanvas.transform.eulerAngles.y - headTracker.eulerAngles.y) > rotationTreshold)
                        _inTransition = true;
                }
            }
        }

        public void SwitchActive(Transform headTracker, Vector3 _startPosition)
        {
            uiCanvas.SetActive(!uiCanvas.activeSelf);
            uiCanvas.transform.rotation = Quaternion.Euler(new Vector3(0, headTracker.eulerAngles.y, 0) + setAngle);
            uiCanvas.transform.position = (_startPosition + 
                                           (Quaternion.Euler(0, uiCanvas.transform.eulerAngles.y, 0) * Vector3.forward * frontOffset) + 
                                           (Vector3.up * upOffset));
        }
    }
}