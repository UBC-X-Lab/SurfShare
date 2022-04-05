using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public float cursor_pos_x;
    public float cursor_pos_y;

    // cursor click
    public int brush_state = 0; // -1: outside, 0: hover, 1: draw (clicked), 2: clear
    public string brush_info = "0,1,1,0";

    //color control
    public Button clearButton;
    public Button blue;
    public Button yellow;
    public Button green;

    private Color BrushColor;

    // the buffer
    public static Queue<string> buffer = new Queue<string>();

    // my lock
    public static readonly object myLock = new object();

    // Start is called before the first frame update
    void Start()
    {
        origin_x = -0.5f;
        origin_y = -3.3f;

        width = 12.0f;
        height = 9.0f;

        clearButton.onClick.AddListener(ClearAll);
        blue.onClick.AddListener(() => SetColor("blue"));
        yellow.onClick.AddListener(() => SetColor("yellow"));
        green.onClick.AddListener(() => SetColor("green"));

        this.BrushColor = new Color(1, 1, 0);
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) // only the panel has collider
        {
            this.cursor_pos_x = Mathf.Abs(hit.point.x - this.origin_x) / this.width;
            this.cursor_pos_y = Mathf.Abs(hit.point.y - this.origin_y) / this.height;
            if (Input.GetMouseButton(0))
            {
                GameObject newBrush = Instantiate(Brush, hit.point - Vector3.forward * 0.1f, hit.collider.gameObject.transform.rotation, transform);
                newBrush.GetComponent<MeshRenderer>().material.color = this.BrushColor;
                this.brush_state = 1;
                //newBrush.transform.localScale = Vector3.one * BrushSize;
            }
            else
            {
                this.brush_state = 0;
            }
        }
        else
        {
            if (this.brush_state == 0 || this.brush_state == 1) // set to outside panel if not button clicked
            {
                this.brush_state = -1;
            }
        }
        brush_info = this.brush_state.ToString() + "," + this.BrushColor.r.ToString() + "," + this.BrushColor.g.ToString() + "," + this.BrushColor.b.ToString();

        lock (myLock)
        {
            buffer.Enqueue(this.cursor_pos_x.ToString() + "," + this.cursor_pos_y.ToString() + "," + this.brush_info);
        }
        //Debug.Log(brush_info);
    }

    void ClearAll()
    {
        GameObject[] paints = GameObject.FindGameObjectsWithTag("paints");
        foreach (GameObject paint in paints)
        {
            Destroy(paint);
        }
        brush_state = 2;
    }

    void SetColor(string color)
    {
        switch (color)
        {
            case "blue":
                this.BrushColor = new Color(0, 0, 1);
                break;
            case "yellow":
                this.BrushColor = new Color(1, 1, 0);
                break;
            case "green":
                this.BrushColor = new Color(0, 1, 0);
                break;
            default:
                this.BrushColor = new Color(0, 0, 1);
                break;
        }
    }
}
