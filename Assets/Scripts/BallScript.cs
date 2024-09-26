using UnityEngine;

public class BallScript : MonoBehaviour
{
    private float destroytime;

    private void Start()
    {
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Ball"), LayerMask.NameToLayer("Ball"));

        destroytime = Random.Range(10f, 15f);
        Destroy(transform.gameObject, destroytime);

        transform.GetComponent<Renderer>().material.color = new Color(0, 0, Random.Range(0.2f, 0.7f));
    }

    public void DestroyBall()
    {
        Destroy(transform.gameObject);
    }
}