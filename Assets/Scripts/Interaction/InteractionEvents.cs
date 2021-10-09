using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interaction
{

    public class InteractionEvents : MonoBehaviour
    {
        public static InteractionEvents current;
        
        // Start is called before the first frame update
        void Awake()
        {
            current = this;
        }

        public event Action onTrackpad;
        public event Action onTrigger;
        public event Action onBack;

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryTouchpad, OVRInput.Controller.Active))
            {
                onTrackpad?.Invoke();
            }

            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.Active))
            {
                onTrigger?.Invoke();
            }
            
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.Active))
            {
                onBack?.Invoke();
            }
        }
    }
}