using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selectable : MonoBehaviour
{
    private bool _selected;
    private bool _clicked;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Spin();
        
        UnsetSelected();
    }

    void Spin()
    {
        if (_clicked)
        {
            transform.Rotate(new Vector3(0, 1, 0));
        }
    }

    public void SetSelected()
    {
        _selected = true;
    }

    public void UnsetSelected()
    {
        _selected = false;
    }
    
    public void Clicked()
    {
        _clicked = !_clicked;
    }
}
