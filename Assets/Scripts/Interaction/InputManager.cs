using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UIElements.Button;


namespace Interaction
{
    public class InputManager : Singleton<InputManager>
    {
        public enum UiNumerations
        {
            TimeUi = 0,
            PauseUi = 1,
            SettingsUi = 2
        }
        
        [SerializeField] private bool inMainScene;
        
        [SerializeField] private Transform headTracker;
        [SerializeField] private Transform remoteTracker;
        
        private LayerMask _interactablesMask; //corresponding layer mask
        [SerializeField] private string interactablesLayer; //layer which all objects which are interactable are on
        
        [SerializeField] private float maxObjectDistance;

        [SerializeField] private OrbitingUI[] uis;

        [SerializeField] private float rotationTreshold; //difference in angle at which ui will follow player head
        
        private LineRenderer _line;
        [SerializeField] private Transform pointer;

        private Orb _lastOrb = null;

        //private RaycastHit[] _raycastBuffer = new RaycastHit[1]; //max one thing
        private RaycastHit _testRaycastHit;
        private RaycastHit _lastRaycastHit;

        private bool _hasHit;
        //public bool clicked;

        private Vector3 _startPosition;

        private Camera _centerCam;
        
        // FMOD
        [Header("FMOD Events")]
        [EventRef] public string HoverEvent = "";
        [EventRef] public string SelectDeselectEvent = "";
        private FMOD.Studio.EventInstance m_SelectDeselect;
        private FMOD.Studio.EventInstance m_Hover;
        private FMOD.Studio.PARAMETER_ID m_SelectDeselectID, m_HoverID;

        // Start is called before the first frame update
        void Awake()
        {
            _interactablesMask = LayerMask.GetMask(interactablesLayer, "UI");
            _line = GetComponent<LineRenderer>();

            _startPosition = headTracker.position;
            _centerCam = headTracker.GetComponent<Camera>();

            if (_line.positionCount != 2)
            {
                print("Amount of positions in line renderer is not 2, check line renderer component, setting to 2");
                _line.SetPositions(new Vector3[2]);
            }

            if (inMainScene)
            {
                InteractionEvents.current.onTrigger += SelectObject;
                InteractionEvents.current.onTrackpad += SwitchHUD;
                InteractionEvents.current.onBack += SwitchPause;
            }
            
            // FMOD
            m_SelectDeselect = RuntimeManager.CreateInstance(SelectDeselectEvent);
            m_Hover = RuntimeManager.CreateInstance(HoverEvent);

            FMOD.Studio.EventDescription selectDeselectEventDescription;
            m_SelectDeselect.getDescription(out selectDeselectEventDescription);
            FMOD.Studio.PARAMETER_DESCRIPTION selectDeselectDescription;
            
            FMOD.Studio.EventDescription hoverEventDescription;
            m_SelectDeselect.getDescription(out hoverEventDescription);
            FMOD.Studio.PARAMETER_DESCRIPTION hoverDescription;

            selectDeselectEventDescription.getParameterDescriptionByName("SelectType", out selectDeselectDescription);
            hoverEventDescription.getParameterDescriptionByName("OrbType", out hoverDescription);

            m_SelectDeselectID = selectDeselectDescription.id;
            m_HoverID = hoverDescription.id;
        }

        // Update is called once per frame
        void Update()
        {
            DrawPointer();

            MoveUI();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                foreach (var orb in FindObjectsOfType<Orb>())
                {
                    orb.ActivateStoryPointInterface();
                }   
            }
        }

        void MoveUI()
        {
            foreach (var ui in uis)
            {
                ui.OrbitOrCheck(headTracker, _startPosition, rotationTreshold);
            }
        }

        void DrawPointer()
        {
            //clicked = false;
            
            //_hasHit = Convert.ToBoolean(FillRayCastBuffer());
            _hasHit = FillRayCastBufferTest();

            _line.SetPosition(0, remoteTracker.position);

            if (!_hasHit)
            {
                _line.SetPosition(1, remoteTracker.position + (remoteTracker.forward * maxObjectDistance));
            }
            else
            {
                _line.SetPosition(1, _testRaycastHit.point);
                if (_testRaycastHit.transform.CompareTag("Orb"))
                {
                    _testRaycastHit.transform.GetComponent<Orb>().IsHovered = true;

                    if (_testRaycastHit.collider != _lastRaycastHit.collider)
                    {
                        m_Hover.setParameterByID(m_HoverID, 1);
                        m_Hover.start();
                    }
                }
                else if (_testRaycastHit.transform.CompareTag("SubOrb"))
                {
                    SmallOrbShaderController temp = _testRaycastHit.transform.GetComponent<SmallOrbShaderController>();
                    temp.IsHovered = true;
                    
                    if (_testRaycastHit.collider != _lastRaycastHit.collider && temp.m_active)
                    {
                        m_Hover.setParameterByID(m_HoverID, 0);
                        m_Hover.start();
                    }
                }
                else
                {
                    if (_testRaycastHit.collider != _lastRaycastHit.collider)
                    {
                        m_Hover.setParameterByID(m_HoverID, 0);
                        m_Hover.start();
                    }
                }
            }

            pointer.position = _line.GetPosition(1);
            pointer.rotation = remoteTracker.rotation;
        }

        void SelectObject()
        {
            if (_hasHit)
            {
                if (_testRaycastHit.transform.CompareTag("Orb"))
                {
                    // save lastorb in order to reset orb when selecting another orb
                    Orb newOrb = _testRaycastHit.transform.GetComponent<Orb>();
                    if (_lastOrb != newOrb && _lastOrb != null)
                    { 
                        _lastOrb.Toggle();
                    }

                    if (newOrb.Toggle())
                    {
                        m_SelectDeselect.setParameterByID(m_SelectDeselectID, 0);
                    }
                    else
                    {
                        m_SelectDeselect.setParameterByID(m_SelectDeselectID, 1);
                    }

                    m_SelectDeselect.start();
                    
                    if (_lastOrb == newOrb)
                    {
                        _lastOrb = null;
                    }
                    else
                    {
                        _lastOrb = newOrb;
                    }
                    
                    
                }
                else if (_testRaycastHit.transform.CompareTag("SubOrb"))
                {
                    //nothing for now
                }
                else
                {
                    _testRaycastHit.transform.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                }
            }

            //clicked = true;
        }

        void SwitchHUD()
        {
            uis[(int) UiNumerations.TimeUi].SwitchActive(headTracker, _startPosition);
        }

        public void SwitchPause()
        {
            uis[(int) UiNumerations.PauseUi].SwitchActive(headTracker, _startPosition);
        }
        
        public void SwitchSettings()
        {
            uis[(int) UiNumerations.SettingsUi].SwitchActive(headTracker, _startPosition);
        }

        /*int FillRayCastBuffer()
        {
            //create raycast from remoteTracker location
            return Physics.RaycastNonAlloc(remoteTracker.position, remoteTracker.forward, _raycastBuffer, maxObjectDistance, _interactablesMask);
        }*/
        
        bool FillRayCastBufferTest()
        {
            //create raycast from remoteTracker location
            _lastRaycastHit = _testRaycastHit;
            return Physics.Raycast(remoteTracker.position, remoteTracker.forward, out _testRaycastHit, maxObjectDistance, _interactablesMask);
        }

        private void OnDestroy()
        {
          ResetInstance();
        }
  }
}