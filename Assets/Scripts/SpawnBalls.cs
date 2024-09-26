using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnBalls : MonoBehaviour
{
    public GameObject scanPoint;
    public Material scanPointMaterial;
    [SerializeField]
    private LayerMask layerMask;

    private Transform camTransform;
    private float planeHeight = 1f;
    private float planeWidth = 1f;
    private float numberOfRaysHeight;
    private float numberOfRaysWidth;
    private Vector3 horizontal;
    private float scannerSizeIncrease = 0.1f;
    private List<MatrixData> matrices = new List<MatrixData>();
    private bool isToggled;

    // Optimization
    private int listSize = 120000;
    private float instanceLifetime = 30f;

    private struct MatrixData
    {
        public Matrix4x4 matrix;
        public float creationTime;
        public Color color;
    }

    private void Start()
    {
        camTransform = Camera.main.transform;
        scanPointMaterial.enableInstancing = true;
        StartCoroutine(RemoveExpiredInstances());
    }

    void Update()
    {
        if (matrices.Count > listSize)
        {
            matrices.RemoveRange(0, matrices.Count - listSize);
        }

        // Adjust plane size
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            planeHeight += scroll * scannerSizeIncrease;
            planeWidth += scroll * scannerSizeIncrease;

            planeHeight = Mathf.Clamp(planeHeight, 0.5f, 3f);
            planeWidth = Mathf.Clamp(planeWidth, 0.5f, 3f);
        }

        // Rotate Line
        if (Input.GetKeyDown(KeyCode.R))
        {
            isToggled = !isToggled; 
        }
        if (isToggled)
        {
            horizontal = camTransform.up;
        }
        else
        {
            horizontal = camTransform.right;
        }

        if (Input.GetKey(KeyCode.Mouse0) && matrices.Count < listSize)
        {
            DrawSquare();
        }
        else if (Input.GetKey(KeyCode.Mouse1) && matrices.Count < listSize)
        {
            DrawLine();
        }
        else if (matrices.Count > listSize)
        {
            // Limit the list size 
            matrices.RemoveRange(0, matrices.Count - listSize);
        }

        DrawScanPointsOnGPU();
    }

    private void DrawSquare()
    {
        numberOfRaysHeight = planeHeight * 3;
        numberOfRaysWidth = planeWidth * 3;

        Vector3 bottomLeft = camTransform.position - camTransform.up * (planeHeight / 2) - camTransform.right * (planeWidth / 2);
        Vector3 bottomRight = camTransform.position - camTransform.up * (planeHeight / 2) + camTransform.right * (planeWidth / 2);
        Vector3 topLeft = camTransform.position + camTransform.up * (planeHeight / 2) - camTransform.right * (planeWidth / 2);
        Vector3 topRight = camTransform.position + camTransform.up * (planeHeight / 2) + camTransform.right * (planeWidth / 2);

        for (int i = 0; i <= numberOfRaysHeight; i++)
        {
            float tHeight = i / numberOfRaysHeight;
            Vector3 pointOnLeftEdge = Vector3.Lerp(bottomLeft, topLeft, tHeight);
            Vector3 pointOnRightEdge = Vector3.Lerp(bottomRight, topRight, tHeight);

            for (int j = 0; j <= numberOfRaysWidth; j++)
            {
                float tWidth = j / numberOfRaysWidth;
                Vector3 rayOrigin = Vector3.Lerp(pointOnLeftEdge, pointOnRightEdge, tWidth);

                float randomOffsetX = Random.Range(-planeWidth / (2 * numberOfRaysWidth), planeWidth / (2 * numberOfRaysWidth));
                float randomOffsetY = Random.Range(-planeHeight / (2 * numberOfRaysHeight), planeHeight / (2 * numberOfRaysHeight));
                rayOrigin += camTransform.right * randomOffsetX + camTransform.up * randomOffsetY;

                Ray ray = new Ray(rayOrigin, camTransform.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, 100f, layerMask))
                {
                    Vector3 scale = new Vector3(0.01f, 0.01f, 0.01f);
                    Matrix4x4 thisMatrix = Matrix4x4.TRS(hit.point, Quaternion.identity, scale);

                    float time = Random.Range(0f, 5f);
                    Color randomBlue = new Color(0f, 0f, Random.Range(0.2f, 0.6f));
                    matrices.Add(new MatrixData { matrix = thisMatrix, creationTime = Time.time + time, color = randomBlue });
                }
            }
        }
    }

    private void DrawLine()
    {
        numberOfRaysWidth = planeWidth * 20;

        Vector3 startPoint = camTransform.position - horizontal * (planeWidth / 2);
        Vector3 endPoint = camTransform.position + horizontal * (planeWidth / 2);

        for (int i = 0; i <= numberOfRaysWidth; i++)
        {
            float tWidth = i / numberOfRaysWidth;
            Vector3 rayOrigin = Vector3.Lerp(startPoint, endPoint, tWidth);

            float randomOffsetX = Random.Range(-planeWidth / (2 * numberOfRaysWidth), planeWidth / (2 * numberOfRaysWidth));
            rayOrigin += horizontal * randomOffsetX;

            Ray ray = new Ray(rayOrigin, camTransform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, layerMask))
            {
                Vector3 scale = new Vector3(0.01f, 0.01f, 0.01f);
                Matrix4x4 thisMatrix = Matrix4x4.TRS(hit.point, Quaternion.identity, scale);

                float time = Random.Range(0f, 5f);
                Color randomBlue = new Color(0f, 0f, Random.Range(0.2f, 0.6f));
                matrices.Add(new MatrixData { matrix = thisMatrix, creationTime = Time.time + time, color = randomBlue });
            }
        }
    }

    private void DrawScanPointsOnGPU()
    {
        // Draw the instanced meshes on gpu
        if (matrices.Count > 0)
        {
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            props.SetVectorArray("_Color", GetColorsArray());
            Graphics.DrawMeshInstanced(scanPoint.GetComponent<MeshFilter>().sharedMesh, 0, scanPointMaterial, GetMatricesArray(), matrices.Count, props); // magic line
        }
    }

    // Remove expired scna point
    private IEnumerator RemoveExpiredInstances()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.01f);
            float currentTime = Time.time;
            matrices.RemoveAll(m => currentTime - m.creationTime > instanceLifetime);
        }
    }

    private Matrix4x4[] GetMatricesArray()
    {
        Matrix4x4[] array = new Matrix4x4[matrices.Count];
        for (int i = 0; i < matrices.Count; i++)
        {
            array[i] = matrices[i].matrix;
        }
        return array;
    }

    private Vector4[] GetColorsArray()
    {
        Vector4[] array = new Vector4[matrices.Count];
        for (int i = 0; i < matrices.Count; i++)
        {
            array[i] = matrices[i].color;
        }
        return array;
    }
}