using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paintable : MonoBehaviour
{
    public GameObject Brush;
    //public float BrushSize = 0.1f;

    // panel stats (set corner as bottom left)
    private float origin_x;
    private float origin_y;

    private float width;
    private float height;

    // cursor pos
    public static float cursor_pos_x;
    public static float cursor_pos_y;

    // cursor click
    public static int clicked = 0;

    // Start is called before the first frame update
    void Start()
    {
        origin_x = -0.5f;
        origin_y = -3.3f;

        width = 12.0f;
        height = 9.0f;
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            cursor_pos_x = Mathf.Abs(hit.point.x - this.origin_x) / this.width;
            cursor_pos_y = Mathf.Abs(hit.point.y - this.origin_y) / this.height;
            if (Input.GetMouseButton(0))
            {
                GameObject newBrush = Instantiate(Brush, hit.point - Vector3.forward * 0.1f, hit.collider.gameObject.transform.rotation, transform);
                clicked = 1;
                //newBrush.transform.localScale = Vector3.one * BrushSize;
            }
            else
            {
                clicked = 0;
            }
        }
    }
}
