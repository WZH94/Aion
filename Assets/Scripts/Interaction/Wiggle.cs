using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class Wiggle : MonoBehaviour
{
    private Vector3 _baseLocalTransform;
    private Vector3 _baseScaleTransform;
    private float _t;
    private float _speed;

    public bool active = true;

    private void Start()
    {
        _baseLocalTransform = transform.localPosition;
        _baseScaleTransform = transform.localScale;
        _speed = UnityEngine.Random.Range(.1f, .3f);
    }

    void Update()
    {
        if (active)
        {
            _t += _speed;
            transform.localPosition = _baseLocalTransform + transform.forward * Mathf.Cos(Mathf.Deg2Rad * (_t));
            transform.localScale = _baseScaleTransform;
        }
        else
        {
            transform.localScale = Vector3.Lerp(_baseScaleTransform, _baseScaleTransform + (.2f * Vector3.one), Time.deltaTime);
        }
    }
}
