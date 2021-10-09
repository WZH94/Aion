using System.Collections;
using System.Collections.Generic;
using Interaction;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIInputModule : BaseInputModule
{
    [SerializeField] private Camera remoteCam;
    [SerializeField] private InputManager inputManager;

    private GameObject _curObject = null;
    private PointerEventData _peData = null;

    protected override void Awake()
    {
        base.Awake();
        
        _peData = new PointerEventData(eventSystem);

        InteractionEvents.current.onTrigger += ButtonClick;
    }

    public override void Process()
    {
        _peData.Reset();
        _peData.position = new Vector2(remoteCam.pixelWidth/2, remoteCam.pixelHeight/2);
        
        eventSystem.RaycastAll(_peData, m_RaycastResultCache);
        _peData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        _curObject = _peData.pointerCurrentRaycast.gameObject;
        
        m_RaycastResultCache.Clear();
        
        HandlePointerExitAndEnter(_peData, _curObject);
    }

    public PointerEventData GetData()
    {
        return _peData;
    }

    private void ButtonClick()
    {
        _peData.pointerPressRaycast = _peData.pointerCurrentRaycast;

        GameObject newPointerPress =
            ExecuteEvents.ExecuteHierarchy(_curObject, _peData, ExecuteEvents.pointerClickHandler);
        
        //can check for null here

        _peData.pressPosition = _peData.position;
        _peData.pointerPress = newPointerPress;
        _peData.rawPointerPress = _curObject;
    }
}
