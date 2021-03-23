using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

public class TestToArray : MonoBehaviour
{
    NativeArray<Vector3> array;
    Vector3[] buffer;
    int count = 3000;
    Mesh mesh;
    private void Awake()
    {
        buffer = new Vector3[count];
        mesh = new Mesh();
        array = new NativeArray<Vector3>(count, Allocator.Persistent);
        array = new NativeArray<Vector3>(count, Allocator.Persistent);
    }

    // Update is called once per frame
    void Update()
    {
        array.CopyTo(buffer);
        mesh.vertices = buffer;
        mesh.GetNativeIndexBufferPtr();
    }
}
