using UnityEngine;
using System;


[ExecuteInEditMode]
public class UniqueId : MonoBehaviour
{
    [SerializeField]
    private string uniqueId;
    public string Id => uniqueId;
    private void Awake()
    {
        if (string.IsNullOrEmpty(uniqueId) && !Application.isPlaying)
        {
            uniqueId = Guid.NewGuid().ToString();
        }
    }
}
